using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using World;

namespace EA
{
    [Serializable]
    class Genome
    {
        public double fitness;
        public int[] weights;
        private Random rand = new Random();

        public Genome(int num_weights, Random rand, int Range)
        {
            this.fitness = 0;
            this.weights = generateWeights(num_weights, rand, Range);
        }

        public double getFitness()
        {
            return this.fitness;
        }

        public int[] getWeights()
        {
            return this.weights;
        }

        public void mutate(double rate, int abs_diff)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                if (rand.NextDouble() < rate)
                {
                    weights[i] += rand.Next(-abs_diff, abs_diff);
                }
            }
        }

        private int[] generateWeights(int number, Random rand, int range)
        {
            // Instantiate an array of double
            int[] a = new int[number];

            // Store random int
            // numbers in the array
            for (int i = 0; i < number; i++)
                a[i] = rand.Next(-range, range);

            return a;
        }
    }

    class GA
    {
        Random rand = new Random();
        int population_size;
        Genome[] population;
        int num_weights;
        int updatePlayerId;
        int weightRange;

        public GA(int updatePlayerId, int population_size, int num_weights, int WeightsRange)
        {
            this.population_size = population_size;
            this.num_weights = num_weights;
            this.updatePlayerId = updatePlayerId;
            this.weightRange = WeightsRange;

            this.population = new Genome[population_size];
            for (int i = 0; i < population_size; i++)
                this.population[i] = new Genome(num_weights, rand, WeightsRange);
        }

        public Genome tournamentSelection(int nr_candidates)
        {
            /// Random Choice of n individuals
            int[] selected = new int[nr_candidates];
            Genome[] genomes = new Genome[nr_candidates];
            int count = 0;

            // While not n individuals have been selected
            while (count < nr_candidates)
            {
                // Get a random selection
                int select = rand.Next(this.population.Length);

                // If new, add
                if (!selected.Contains(select))
                {
                    genomes[count] = this.population[select];

                    // Save selected
                    selected[count] = select;
                    count++;
                }
            }

            /// Select best individual
            int best_ind = 0;

            for (int i = 1; i < nr_candidates; i++)
                if (genomes[best_ind].getFitness() < genomes[i].getFitness())
                    best_ind = i;

            /// Return best individual
            return genomes[best_ind];
        }

        public Genome crossOver(Genome gen1, Genome gen2, double rate)
        {
            Genome[] genomes;
            Genome new_genome = new Genome(gen1.getWeights().Length, rand, this.weightRange);

            if (rand.NextDouble() < .5)
                genomes = new Genome[] { gen1, gen2 };
            else
                genomes = new Genome[] { gen2, gen1 };

            if (rand.NextDouble() < rate)
            {
                int split = rand.Next(gen1.getWeights().Length);

                new_genome.weights = mergeWeights(genomes, split);
            }
            else
            {
                new_genome = genomes[0].DeepClone();
            }

            return new_genome;
        }

        public double getFitnessGenome(Genome g)
        {
            return g.getFitness();
        }

        public int[] mergeWeights(Genome[] genomes, int split)
        {
            int select = 0;
            int[] result = new int[genomes[0].getWeights().Length];
            for (int i = 0; i < genomes[0].getWeights().Length; i++)
            {
                if (i == split)
                {
                    select = 1;
                }

                result[i] = genomes[select].getWeights()[i];
            }
            //population.OrderByDescending(x => x.fitness);
            return result;
        }

        public void upgradeGeneration(double crossOverRate, double mutateRate, int mutateDiff, int candidatesTS, bool maximize)
        {
            // Declare variables
            Genome gen1, gen2;

            // Sort population based on fitness
            if (maximize)
                this.population = this.population.OrderByDescending(x => x.fitness).ToArray();
            else
                this.population = this.population.OrderByDescending(x => -x.fitness).ToArray();

            // Elitism, keep best genomes
            Genome[] new_genomes = new Genome[this.population_size];
            new_genomes[0] = this.population[0];
            new_genomes[1] = this.population[1];
            new_genomes[2] = this.population[2];
            new_genomes[3] = this.population[3];

            // Cross-over between best-second, best-third, ..., second-third, etc.
            new_genomes[4] = crossOver(this.population[0], this.population[1], crossOverRate);
            new_genomes[5] = crossOver(this.population[0], this.population[2], crossOverRate);
            new_genomes[6] = crossOver(this.population[0], this.population[3], crossOverRate);
            new_genomes[7] = crossOver(this.population[1], this.population[2], crossOverRate);
            new_genomes[8] = crossOver(this.population[1], this.population[3], crossOverRate);
            new_genomes[9] = crossOver(this.population[2], this.population[3], crossOverRate);

            // All the others, tournament selection
            for (int i = 10; i < this.population_size; i++)
            {
                gen1 = tournamentSelection(candidatesTS);
                gen2 = tournamentSelection(candidatesTS);
                new_genomes[i] = crossOver(gen1, gen2, crossOverRate);
            }

            // Mutate all, except the first one
            for (int i = 1; i < this.population_size; i++)
            {
                new_genomes[i].mutate(mutateRate, mutateDiff);
            }

            // Save new genomes
            this.population = new_genomes;

            // Reset fitness
            for (int i = 0; i < this.population_size; i++)
            {
                this.population[i].fitness = 0;
            }
        }

        public void run(int num_gen, int num_games)
        {
            // Set up game loop
            GameLoop gl = new GameLoop();

            for (int gen = 1; gen < num_gen + 1; gen++)
            {
                for (int genome = 0; genome < this.population_size; genome++)
                {
                    // Change weights used
                    gl.setupSimulate(this.population[genome].weights, this.updatePlayerId);

                    // Determine fitness
                    //this.population[genome].fitness = Cost(this.population[genome].weights);
                    this.population[genome].fitness = (float)gl.playGamesOutput(num_games, false, this.population[genome].weights, this.updatePlayerId)[this.updatePlayerId] / (float)num_games;

                    Console.WriteLine($"Generation {gen} - Genome {genome} - Weight {this.population[genome].weights.toPrint()}, fitness: {this.population[genome].fitness}.");

                    if (this.population[genome].fitness == 1)
                    {
                        this.population[genome].fitness += (float)gl.playGamesOutput(num_games, false, this.population[genome].weights, this.updatePlayerId)[this.updatePlayerId] / (float)num_games / 10;
                        Console.WriteLine($"Generation {gen} - Genome {genome} - Weight {this.population[genome].weights.toPrint()}, fitness: {this.population[genome].fitness}.");
                    }
                }

                // Print results
                double maxVal = this.population.Max(x => x.fitness);
                if (gen % 1 == 0)
                    Console.WriteLine("Generation " + gen + ". Best Fitness: " + formatString(maxVal) + ". Weights: " +
                        string.Join(",", this.population.First(x => x.fitness == maxVal).getWeights()));
                else
                    Console.WriteLine("Generation " + gen + ". Best Fitness: " + formatString(maxVal) + ".");

                // Upgrade population
                upgradeGeneration(.95, .95, 3, 3, true);
            }
        }

        private string formatString(double val)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            return String.Format(val % 1 == 0 ? "{0:0}" : "{0:0.00}", val);
        }
    }
}
