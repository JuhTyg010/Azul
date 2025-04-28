#!/bin/bash

trainer_path="/home/juhtyg/Desktop/Azul/AzulCLI/AzulTrainer/bin/Debug/net8.0"
running_dir="/home/juhtyg/Desktop/Azul/AzulCLI/AzulTrainer/bin/Debug/net8.0"
working_dir="/home/juhtyg/Desktop/Azul"

bots=(PPO)
opponents=(random heuristic)
numbers_from_r=(0 1 2 3)  # Replace this with the actual numbers you want to use

# Loop through all combinations

for bot1 in "${bots[@]}"; do
  for bot2 in "${opponents[@]}"; do
    for r in "${numbers_from_r[@]}"; do
      # Construct the specific directory
      specific_dir="${working_dir}/${r}/${bot1}_${bot2}/"
     
      # Check if the directory exists, if not, create it
      if [ ! -d "$specific_dir" ]; then
        echo "Directory $specific_dir does not exist. Creating it."
        mkdir -p "$specific_dir"  # Create the directory recursively
      fi

      # Create the argument string for the trainer program
      args="-m 0 -r ${r} -d ${specific_dir} -l '${bot1} ${bot2}' -c 100000"
     
	cmd=(
        "${trainer_path}/AzulTrainer"
        -m 0
        -r "${r}"
        -d "${specific_dir}"
        -l "${bot1} ${bot2}"  # This will be treated as one argument
        -c 100000
      )

      # Run the trainer program in the background for parallel execution
      echo "Running command: ${trainer_path}/AzulTrainer $args"
      cd ${running_dir}
      "${cmd[@]}"  # Adjust the program name as necessary
    done
  done
done

# Wait for all background processes to finish
wait

echo "All tasks completed!"
