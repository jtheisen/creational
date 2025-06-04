create temp view rank_puddle_roots as (
  select
    t.item_number,
    t.name,
    t.rank,
    t.rank_level
  from 'taxonomy.parquet' t
  join 'taxonomy.parquet' p on t.parent_item_number = p.item_number
  where t.is_rank_puddle and not p.is_rank_puddle
  order by t.item_number
);

copy rank_puddle_roots to 'rank_puddle_roots.parquet' (format 'parquet');
