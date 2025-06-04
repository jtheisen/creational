from snakemake.script import snakemake
import duckdb

if (len(snakemake.output)) < 1:
    raise ValueError("Expected a least one item in output.")
    
sql_files = [f for f in snakemake.input if f.endswith(".sql")]
non_sql_files = [f for f in snakemake.input if not f.endswith(".sql")]

if len(sql_files) != 1:
    raise ValueError("Expected exactly one .sql file.")

if len(non_sql_files) == 0:
    raise ValueError("Expected at least one input file.")

sql_file = sql_files[0]

sql_template = ""

with open(sql_file, "r", encoding="utf-8") as f:
    sql_template = f.read()

con = duckdb.connect()

con.execute(sql_template)
