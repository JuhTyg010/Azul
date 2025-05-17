using Azul;

namespace Genetic;

public class Trainer {
    private readonly Random _random = new();
    private List<Agent> _population;

    public Trainer(int populationSize) {
        _population = InitializePopulation(populationSize);
    }

    private List<Agent> InitializePopulation(int size) {
        var population = new List<Agent>();
        for (var i = 0; i < size; i++) {
            var weights = new Dictionary<string, double>();
            
            var obj = new Rules();
            var methods = typeof(Rules).GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(Genetic.Rule), false).Any());

            foreach (var method in methods) {
                string methodName = method.Name;
                weights.TryAdd(methodName, (double) _random.NextDouble() * 4 - 2);
            }
            population.Add(new Agent(weights));
        }

        return population;
    }

    public void RunGeneration(Board[] gameScenarios) {
        // Evaluate fitness by simulating games
        foreach (var agent in _population) {
            //agent.Fitness = SimulateGames(agent, gameScenarios);
        }

        // Select top agents (e.g., top 20%)
        var topAgents = _population.OrderByDescending(a => a.Fitness).Take(_population.Count / 5).ToList();

        // Breed new generation
        _population = BreedNewGeneration(topAgents);
    }

    private List<Agent> BreedNewGeneration(List<Agent> parents) {
        var newGeneration = new List<Agent>();

        // Keep top performers
        newGeneration.AddRange(parents);

        // Crossover and mutate
        while (newGeneration.Count < _population.Count) {
            var parent1 = parents[_random.Next(parents.Count)];
            var parent2 = parents[_random.Next(parents.Count)];
            var childWeights = Crossover(parent1, parent2);
            Mutate(childWeights);
            newGeneration.Add(new Agent(childWeights));
        }

        return newGeneration;
    }

    private Dictionary<string, double> Crossover(Agent parent1, Agent parent2) {
        var childWeights = new Dictionary<string, double>();
        foreach (var rule in parent1.RuleWeights.Keys)
            // Blend weights from both parents
            childWeights[rule] = (parent1.RuleWeights[rule] + parent2.RuleWeights[rule]) / 2;
        return childWeights;
    }

    private void Mutate(Dictionary<string, double> weights) {
        foreach (var rule in weights.Keys.ToList())
            if (_random.NextDouble() < 0.1) // 10% mutation chance
                weights[rule] += (float) (_random.NextDouble() * 0.4 - 0.2); // Small perturbation
    }
}