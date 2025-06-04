import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq
import numpy as np
import io
import shutil
import subprocess
import re
import sys
import os
import argparse
import json

# Tree source: https://tree.opentreeoflife.org/about/synthesis-release/v13.4
# https://files.opentreeoflife.org/ott/ott3.3/ott3.3/taxonomy.tsv

# Flags that make a taxon be excluded for TNRS - we should probably exclude those as well
# https://github.com/OpenTreeOfLife/reference-taxonomy/blob/master/doc/taxon-flags.md
excludeWithFlags = [
    "not_otu",
    "environmental",
    "environmental_inherited",
    "viral",
    "hidden",
    "hidden_inherited",
    "was_container",

    # This one only we exclude
    "extinct"
]

ranksInOrder = [
    "domain",
    "kingdom",
    "subkingdom",
    "infrakingdom",
    "superphylum",
    "phylum",
    "subphylum",
    "infraphylum",
    "superclass",
    "class",
    "subclass",
    "infraclass",
    "subterclass",
    "cohort",
    "subcohort",
    "superorder",
    "order",
    "suborder",
    "infraorder",
    "parvorder",
    "superfamily",
    "family",
    "subfamily",
    "tribe",
    "subtribe",
    "genus",
    "subgenus",
    "section",
    "subsection",
    "species group",
    "species subgroup",
    "species",
    "subspecies",
    "variety/varietas",
    "forma",

    "no rank",
    "no rank - terminal"
]

ranksSpecial = [
    "domain",
    "phylum",
    "class",
    "order",
    "family",
    "genus",
    "species"
]

forced_files = []
force_all = False

special_rank_mod = 10

def get_ranks(includeNoRanks=False):
    ranks = {}
    i = 0
    j = 0
    m = special_rank_mod
    for rank in ranksInOrder:
        isNoRank = "no rank" in rank
        if j < len(ranksSpecial) and ranksSpecial[j] == rank:
            j += 1
            i = j * m
        elif (i + 1) % m == 0:
            raise Exception("Not enough numbers between special ranks")
        elif not isNoRank:
            i += 1
        v = i
        if isNoRank:
            v = -1
        if not isNoRank or includeNoRanks:
            aliases = rank.split('/')
            for alias in aliases:
                ranks[alias] = v
    return ranks

def get_get_is_special_rank():
    ranks = get_ranks()
    specialRankLevels = set()
    for rank in ranksSpecial:
        specialRankLevels.add(ranks[rank])
    def get_is_special_rank(rank_level):
        return rank_level in specialRankLevels
    return get_is_special_rank

def process_tree(filter_excluded=False):
    input_file = "taxonomy_annotated.parquet"

    print(f"Reading file: {input_file}")

    table = pq.read_table(input_file)

    print("Convert to numpy")

    def get_column(name):
        return table[name].to_numpy(zero_copy_only=False)
        
    col_is_root = get_column("is_root")
    col_uid = get_column("uid")
    col_parent_uid = get_column("parent_uid")
    col_name = get_column("name")
    col_rank = get_column("rank")
    col_excluded = get_column("excluded_any")

    root_count = np.sum(col_is_root == 1)

    if (root_count != 1):
        raise Exception(f"Root count is unexpectedly {root_count}")

    root_i = np.argmax(col_is_root == 1)

    print(f"Using {col_name[root_i]} as root")

    source_rows = len(col_uid)
    max_uid = np.max(col_uid) + 1

    print(f"Read {source_rows} rows with max uid being {max_uid}, now building tree")

    first_child_i_by_uid = np.zeros(max_uid, dtype=np.int32)
    item_i_by_uid = np.zeros(max_uid, dtype=np.int32)
    next_sibling_i_by_i = np.zeros(source_rows, dtype=np.int32)

    for i in range(source_rows):
        uid = col_uid[i]
        if item_i_by_uid[uid]:
            raise Exception(f"uid {uid} is set more than once in the source")
        item_i_by_uid[uid] = i
        parent_uid = col_parent_uid[i]
        if parent_uid < 0:
            continue
        try:
            previous_first_child_i = first_child_i_by_uid[parent_uid]
        except:
            print(f"parent_uid was {parent_uid} with type {type(parent_uid)}")
            raise
        first_child_i_by_uid[parent_uid] = i
        next_sibling_i_by_i[i] = previous_first_child_i    

    print("Descending tree")

    current_item_number = -1

    uids = np.zeros(source_rows, dtype=np.int32)
    dfs_item_numbers = np.zeros(source_rows, dtype=np.int32)
    dfs_parent_item_numbers = np.zeros(source_rows, dtype=np.int32)
    child_counts = np.zeros(source_rows, dtype=np.int32)
    depths = np.zeros(source_rows, dtype=np.int32)
    relative_depths = np.zeros(source_rows, dtype=np.int32)
    descendant_counts = np.zeros(source_rows, dtype=np.int32)
    excluded_descendants = np.zeros(source_rows, dtype=np.bool)
    declared_rank_levels = np.zeros(source_rows, dtype=np.int32)
    rank_levels = np.zeros(source_rows, dtype=np.int32)
    rank_presences = np.zeros(source_rows, dtype=np.int64)
    is_reusing_rank = np.zeros(source_rows, dtype=np.bool)
    is_rank_puddle = np.zeros(source_rows, dtype=np.bool)

    was_i_visited = np.zeros(source_rows, dtype=np.bool)

    ranks = get_ranks(includeNoRanks=True)

    get_is_special_rank = get_get_is_special_rank()

    def indent(depth):
        return ' ' * depth

    def get_is_ranked_in_word(rank_level):
        return 0 <= rank_level and rank_level < 63

    def descend(
            i,
            parent_i=-1,
            parent_item_number=-1,
            depth=0,
            relative_depth=0,
            excluded_decendant=False,
            rank_presence=0,
            parent_rank_level=0,
            enable_logging=False
            ):
        nonlocal current_item_number

        if was_i_visited[i]:
            raise Exception(f"Taxon at source position {i} visisted a second time")

        was_i_visited[i] = True

        if col_excluded[i]:
            if enable_logging:
                print(f"{indent(depth)}at source i #{i} pi #{parent_i}: skipping")
            excluded_decendant = True
            if filter_excluded:
                return 0

        try:
            current_item_number += 1
            item_number = current_item_number

            depths[item_number] = depth
            uid = col_uid[i]
            uids[item_number] = uid
            dfs_item_numbers[item_number] = item_number
            dfs_parent_item_numbers[item_number] = parent_item_number
            excluded_descendants[item_number] = excluded_decendant
            rank_level = ranks.get(col_rank[i]) or -2
            declared_rank_levels[item_number] = rank_level

            is_ranked_in_word = get_is_ranked_in_word(rank_level)
            if is_ranked_in_word and (rank_presence & (1 << rank_level)) != 0:
                is_reusing_rank[item_number] = True

            if get_is_special_rank(rank_level):
                relative_depth = 0
            if rank_level >= 0:
                if rank_level == parent_rank_level:
                    is_rank_puddle[item_number] = is_rank_puddle[parent_item_number] = True
                elif rank_level < parent_rank_level and parent_rank_level >= 0:
                    rank_level = -3
                elif is_ranked_in_word:
                    rank_presence |= (1 << rank_level)
            relative_depths[item_number] = relative_depth
            rank_levels[item_number] = rank_level
            rank_presences[item_number] = rank_presence

            total = 0
            child_i = first_child_i_by_uid[uid]
            child_count = 0
            if enable_logging:
                if child_i:
                    print(f"{indent(depth)}at source i #{i} pi #{parent_i} target #{item_number} uid {uid} puid {parent_uid} is a parent of some children, descending")
                else:
                    print(f"{indent(depth)}at source i #{i} pi #{parent_i} target #{item_number} uid {uid} puid {parent_uid} is a leaf")
            while child_i:
                child_total = descend(
                    child_i,
                    i,
                    item_number,
                    depth + 1,
                    relative_depth + 1,
                    excluded_decendant,
                    rank_presence,
                    rank_level,
                    enable_logging
                )
                if child_total > 0:
                    child_count += 1
                total += child_total
                child_i = next_sibling_i_by_i[child_i]
            if enable_logging and child_count > 0:
                print(f"{indent(depth)}at source #{i} target #{item_number} uid {uid} aescended with {child_count} children and {total} descendants")
            child_counts[item_number] = child_count
            descendant_counts[item_number] = total

            return total + 1
        except:
            print(f"{indent(depth)}at uid {uid}")
            raise

    final_rows = descend(root_i)

    if (current_item_number != current_item_number):
        raise Exception("Unexpected row count after recursion")

    print("Writing out structural data")

    structure_table = pa.table({
        'uid': uids,
        'item_number': dfs_item_numbers,
        'parent_item_number': dfs_parent_item_numbers,
        'child_count': child_counts,
        'depth': depths,
        'relative_depth': relative_depths,
        'descendant_count': descendant_counts,
        'excluded_descendant': excluded_descendants,
        'declared_rank_level': declared_rank_levels,
        'rank_level': rank_levels,
        'rank_presence': rank_presences,
        'is_rank_puddle': is_rank_puddle,
        'is_reusing_rank': is_reusing_rank
    })

    pq.write_table(structure_table[:final_rows], 'taxonomy_structure.parquet')

    excluded_rows = source_rows - final_rows
    excluded_rows_percent = 100.0 * excluded_rows / source_rows

    print(f"Strucutal data constructed, {excluded_rows} of {source_rows} taxons excluded ({excluded_rows_percent:.0f}%)");

def check_existence(step, out_file):
    global force_all
    pure_name = os.path.splitext(out_file)[0]
    if os.path.exists(out_file):
        if force_all or pure_name in forced_files or out_file in forced_files:
            print(f"{step}: {out_file} exists, but we're forcing a rebuild")
            force_all = True
            return True
        else:
            print(f"{step}: {out_file} exists, skipping")
            return False
    else:
        print(f"{step}")
        return True

def write_rank_file():
    ranks = get_ranks(includeNoRanks=False)
    data = {
        "ranks": ranks,
        "special": ranksSpecial
    }
    with open('ranks.json', 'w') as f:
        json.dump(data, f)

def main(is_snakemake):
    global forced_files

    if is_snakemake:
        make_full = snakemake.params["full"]
    if not is_snakemake:
        parser = argparse.ArgumentParser(description='Convert and enrich an open tree taxonomy file (.tsv)')
        parser.add_argument('-f', '--force', help='Force recreating a specific intermediary file', action='append')
        parser.add_argument('-t', '--full', help='Run a test', action='store_true')
        parser.add_argument('-t', '--test', help='Run a test', action='store_true')
        parser.add_argument('-c', '--compression', 
                        choices=['snappy', 'gzip', 'brotli', 'lz4', 'zstd', 'none'],
                        default='brotli',
                        help='Compression algorithm (default: brotli, reading the tree is slow anyway)')
        
        args = parser.parse_args()

        make_full = args.full

        forced_files = args.force or []

        if args.test:
            print(get_ranks(includeNoRanks=True))
            return

    if check_existence("Constructing structural data", "taxonomy_structure.parquet"):
        process_tree(not make_full)

    write_rank_file()

    print("Done!")

is_snakemake = 'snakemake' in globals()

if __name__ == '__main__' or is_snakemake:
    main(is_snakemake)
