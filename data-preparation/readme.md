# Conda

This requires condaforge and a conda env properly set up to run.

```sh
# Initialize conda once per conda installation
conda init

# Create the env once for this project
conda env create -f environment.yml

# This needs to run for every new shell
conda activate creational

# This shows all env and marks the active one
conda env list

# This installs new packages when the environment file
# was edited - no removal will take place though
conda env update -f environment.yml
```
