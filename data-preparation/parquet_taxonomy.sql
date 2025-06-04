create table tax as select * from read_csv_auto('taxonomy_stripped.tsv', delim='|');

create temp view result as (
  select
    uid, coalesce(parent_uid, -1) parent_uid, name, rank, flags
  from tax
  order by uid
);

copy result to 'taxonomy_source.parquet' (format 'parquet');