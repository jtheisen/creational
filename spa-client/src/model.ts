import * as aq from "arquero";
import invariant from "tiny-invariant";
import * as _ from "lodash";

/**
 * - rank id and rank level are synonymous and should be consolidated - and levelId is something different
 * - item number should be renamed to lindex
 */

//const rootI = 0; // cellular organism
//const rootI = 2169140; // Carnivora

function invertIntoArray(source: { [k: string]: number }) {
  const result: string[] = [];
  for (let k in source) {
    result[source[k]] = k;
  }
  return result;
}

function invertFromArray(source: string[]) {
  const result: { [k: string]: number } = {};
  for (let k in source) {
    result[source[k]] = k as any as number;
  }
  return result;
}

function createRankings(sourceRanks: SourceRanks) {
  const { ranks, special } = sourceRanks;

  const rankById = invertIntoArray(ranks);

  const isRankSpecialById = _.map(rankById, (r) => special.indexOf(r) >= 0);

  const specialRankIds = _.chain(special)
    .map((r) => sourceRanks.ranks[r])
    .uniq()
    .orderBy((r) => r)
    .value();

  const specialRankIdOrders: number[] = [];
  for (let r in rankById) {
    specialRankIdOrders[r] = specialRankIds.indexOf(Number(r));
  }

  // console.info({
  //   ranks,
  //   rankById,
  //   ranksByIdKeys: Object.keys(rankById),
  //   specialRankIdOrders,
  //   specialRankIds,
  // });

  if (_.some(specialRankIds, (i) => typeof i !== "number")) {
    throw Error(`Special rank has no id`);
  }

  function getSpecialRankIdPercentageOrNot(l: number) {
    const order = specialRankIdOrders[l];
    if (order === undefined || order < 0) {
      return null;
    }
    return (1.0 / specialRankIds.length) * order;
  }

  function createRankObjectOrNull(level: number) {
    invariant(typeof level === "number");

    if (level < 0) {
      return null;
    }
    const name = rankById[level];
    if (!name) {
      throw Error(`Unkown rank level ${level}`);
    }
    const isSpecial = isRankSpecialById[level];

    const specialRankLevelPercentage = getSpecialRankIdPercentageOrNot(level);

    return {
      level,
      name,
      isSpecial,
      specialRankLevelPercentage,

      toString() {
        return `${name}`;
      },
    };
  }

  return {
    sourceRanks,
    rankById,
    rankIdsByName: ranks,
    specialRanks: special,
    isRankSpecialById,
    specialRankIds,
    specialRankIdOrders,
    getRankObjectOrNull: _.memoize(createRankObjectOrNull),
    getSpecialRankIdPercentageOrNot,
  };
}

export type Rankings = ReturnType<typeof createRankings>;
export type Rank = NonNullable<ReturnType<Rankings["getRankObjectOrNull"]>>;

interface SourceRanks {
  ranks: { [rank: string]: number };
  special: string[];
}

export async function fetchModel() {
  console.info("Loading data");

  const [
    childCounts,
    rank,
    sourceRanks,
    names,
    parent_for_debugging,
    rank_puddle_roots,
  ] = await Promise.all([
    fetchArray(`/child_count.bin`),
    fetchArray(`/rank.bin`),
    fetchJson<SourceRanks>(`/ranks.json`),
    fetchJson<string[]>(`/names.json`),
    fetchArray(`/parent_for_debugging.bin`),
    fetchArray(`/rank_puddle_roots.bin`),
  ]);

  console.info(`All data for ${childCounts.length} taxons loaded`);

  const nameIs = invertFromArray(names);

  console.info(`Inversions built, now building tree`);

  const sourceTree = aq.table(
    createSourceTree({
      childCounts,
      rankLevels: rank,
      parent_for_debugging,
      names_for_debugging: names,
    })
  );

  const rankings = createRankings(sourceRanks);

  console.info(`Tree ready`);

  return {
    sourceRanks,
    sourceTree,
    rankings,
    names,
    nameIs,
    rankPuddleRoots: rank_puddle_roots,
  };
}

export type Model = Awaited<ReturnType<typeof fetchModel>>;

async function fetchJson<T>(name: string) {
  const dataRequest = await fetch(name);
  const data = await dataRequest.json();
  return data as T;
}

async function fetchArray(name: string) {
  const dataRequest = await fetch(name);
  const dataBuffer = await dataRequest.arrayBuffer();
  const data = new Int32Array(dataBuffer);
  return data;
}

function undefineMissingRankLevel(rankLevel: number) {
  return rankLevel >= 0 ? rankLevel : undefined;
}

interface CreateSourceTreeParams {
  childCounts: Int32Array;
  rankLevels: Int32Array;
  parent_for_debugging: Int32Array;
  names_for_debugging?: string[];
}

type ElementType<T> = T extends (infer U)[]
  ? U
  : T extends ReadonlyArray<infer U>
  ? U
  : T extends ArrayLike<infer U>
  ? U
  : never;

type ElementTypesOf<T> = {
  [K in keyof T]: ElementType<T[K]>;
};

export type Raw = ElementTypesOf<ReturnType<typeof createSourceTree>>;

// Construct the complete source tree with depth, rank_level, etc.
function createSourceTree(params: CreateSourceTreeParams) {
  const { childCounts, rankLevels, parent_for_debugging } = params;

  invariant(childCounts, "childCounts is missing");
  invariant(rankLevels, "rank is missing");

  const n = childCounts.length;

  const item_number = new Uint32Array(n);
  const parent_item_number = new Int32Array(n);
  const descendant_count = new Uint32Array(n);
  const depth = new Uint32Array(n);
  const rank_smear_top_item_number = new Uint32Array(n);

  let maxRankLevel = -Infinity;
  for (let i = 0; i < rankLevels.length; i++) {
    if (rankLevels[i] > maxRankLevel) maxRankLevel = rankLevels[i];
  }
  const current_rank_smear_top_item_numbers = new Uint32Array(maxRankLevel);

  let nextI = 0;

  function descend(
    parentI: number | undefined = undefined,
    currentDepth = 0,
    currentInterRankDepth = 0,
    enableLogging = false
  ) {
    const currentI = nextI++;
    if (currentI >= n) {
      throw Error(
        `Exhausted all item even though children were left to descend to`
      );
    }
    const verifiedParentI = parent_for_debugging[currentI];
    if (parentI !== undefined && verifiedParentI !== parentI) {
      throw Error(
        `Child #${currentI} has constructed parent ${parentI} but ${verifiedParentI} in the verification file`
      );
    }
    item_number[currentI] = currentI;
    parent_item_number[currentI] = parentI ?? -1;
    depth[currentI] = currentDepth;
    const numberOfChildren = childCounts[currentI];
    const rankLevel = undefineMissingRankLevel(rankLevels[currentI]);
    if (rankLevel !== undefined) {
      if (!current_rank_smear_top_item_numbers[rankLevel]) {
        current_rank_smear_top_item_numbers[rankLevel] = currentI;
      }
      rank_smear_top_item_number[currentI] =
        current_rank_smear_top_item_numbers[rankLevel];
    }
    let numberOfDescendants = 0;
    if (enableLogging) {
      if (numberOfChildren > 0) {
        console.info(
          `#${currentI} is a parent of ${numberOfChildren} children, descending`
        );
      } else {
        console.info(`#${currentI} is a leaf`);
      }
    }
    for (let c = 0; c < numberOfChildren; ++c) {
      numberOfDescendants += descend(
        currentI,
        currentDepth + 1,
        currentInterRankDepth + 1,
        enableLogging
      );
    }
    if (
      rankLevel !== undefined &&
      current_rank_smear_top_item_numbers[rankLevel] === currentI
    ) {
      current_rank_smear_top_item_numbers[rankLevel] = 0;
    }
    if (enableLogging && numberOfChildren > 0) {
      console.info(
        `#${currentI} back from ${numberOfChildren} children with ${numberOfDescendants} descendants (next will be ${nextI})`
      );
    }
    descendant_count[currentI] = numberOfDescendants;
    return numberOfDescendants + 1;
  }

  descend();

  if (!descendant_count[0]) {
    throw Error(`The top descendant count was not set`);
  }

  return {
    item_number,
    parent_item_number,
    child_count: childCounts,
    descendant_count,
    depth,
    rank_level: rankLevels,
    rank_smear_top_item_number,
  };
}
