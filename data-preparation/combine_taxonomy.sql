create temp view result as (
  select
    f.*,
    s.* exclude (uid)
  from 'taxonomy_structure.parquet' s
  join 'taxonomy_annotated.parquet' f on f.uid = s.uid
  order by s.item_number
);

-- FIXME: we need to record which taxons have a ranking
-- that is inconsistent with the tree structure
--with cte as (
--  select * from 'taxonomy.parquet'
--)
--select p.name, p.rank, n.name, n.rank
--from cte n
--join cte p on n.parent_uid = p.uid
--where n.rank_level >= 0 and p.rank_level >= 0 and n.rank_level < p.rank_level -- should never happen
--order by p.name, n.name;

copy result to 'taxonomy.parquet' (format 'parquet');