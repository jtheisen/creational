-- Get how many species are in one of each of the respective ranks
with source as (
  select *
  from 'taxonomy.parquet'
), ranks as (
  select rank, rank_level
  from source
  where rank_level >= 0 and rank_level < 63
  group by rank, rank_level
), leaves as (
  select *
  from source
  where rank = 'species'
), by_rank as (
  select *
  from ranks r
  join lateral (
    select count(*) c
    from leaves
    where rank_presence & (1::BIGINT << r.rank_level) <> 0
  ) l on true
)
select *, (select count(*) from leaves) total
from by_rank
order by c desc;
