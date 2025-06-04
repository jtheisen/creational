Definitions and todos

# rank_level

Rename to rank_number; between 0 and 63 for well-ranked taxons and <0 otherwise; special values are

-1: unranked in source
-2: was ranked but rank was unknown
-3: was ranked but rank was higher than that of ancestors

# item_number

Rename to lindex; local index of the taxonomy working array, such as taxonomy.parquet or the tree source data from that in js

# level and levelId

A number constructed from the rank_level and interrank-depths and it's hex-stringification
