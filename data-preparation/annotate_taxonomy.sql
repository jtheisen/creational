create temp view step0 as (
  select
    *,
    length(name) - length(replace(name, ' ', '')) + 1 name_parts
  from 'taxonomy_source.parquet'
);

create temp view step1 as (
  select
    uid, coalesce(parent_uid, -1) parent_uid, name, rank, flags,
    name = 'cellular organisms' is_root,
    flags like '%extinct%' excluded_extinct,
    exists (
      from '../taxon_exclude_flags.csv'
      select 1
      where strpos(flags, exclude_flag) > 0
    ) excluded_excluded_flags,
    name_parts > 2 excluded_sub_species,
    regexp_matches(name, '\d$') excluded_ends_in_digit
  from step0
  order by uid
);

create temp view step2 as (
  select
    *,
      excluded_extinct or
      excluded_excluded_flags or
      excluded_sub_species or
      excluded_ends_in_digit
    excluded_any
  from step1
);

copy step2 to 'taxonomy_annotated.parquet' (format 'parquet');
