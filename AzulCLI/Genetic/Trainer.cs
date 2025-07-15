using Azul;
using NonML;

namespace Genetic;

public class Trainer {
    private readonly Random _random = new();
    private List<Agent> _population;
    private int[] _flagCount;
    private bool _wasGameKilled = false;

    public Trainer(int populationSize) {
        _population = InitializePopulation(populationSize);
        _flagCount = new int[_population.Count];
    }

    public Trainer(double margin, double limit) {
        _population = InitializePopulation(margin, limit);
        _flagCount = new int[_population.Count];

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
    
    private List<Agent> InitializePopulation(double margin, double limit)
    {
        var population = new List<Agent>();

        var obj = new Rules();
        var methods = typeof(Rules).GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(Genetic.Rule), false).Any())
            .ToList();

        int ruleCount = methods.Count;
        if (ruleCount == 0) return population;

        var values = new List<double>();
        for (double v = -limit; v <= limit + 1e-8; v += margin) // 1e-8 handles floating-point precision
            values.Add(Math.Round(v, 6)); // rounding for safety

        var combinations = CartesianProduct(Enumerable.Repeat(values, ruleCount).ToList());

        foreach (var combo in combinations)
        {
            var weights = new Dictionary<string, double>();
            for (int i = 0; i < ruleCount; i++)
            {
                weights[methods[i].Name] = combo[i];
            }
            population.Add(new Agent(weights));
        }

        return population;
    }

    private List<List<T>> CartesianProduct<T>(List<List<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
        foreach (var sequence in sequences)
        {
            result = result.SelectMany(
                acc => sequence,
                (acc, item) => acc.Append(item)
            );
        }
        return result.Select(r => r.ToList()).ToList();
    }

    public void RunGeneration(int playerCount) {
        int limiter = _population.Count / 5;
        var semaphore = new SemaphoreSlim(32);
        var rnd = new Random();
        var tasks = new List<Task>();

        object fitnessLock = new object(); // Lock for updating fitness

        for (int i = 0; i < _population.Count; i++) {
            _flagCount[i] = 0;
        }

        for (int i = 0; i < _population.Count; i++) {
            if (_flagCount[i] > 2) continue;

            for (int j = i + 1; j < _population.Count; j++) {
                if (_flagCount[j] > 2) continue;
                if (_flagCount[i] > 0 && _flagCount[j] > 0) continue;

                
                semaphore.Wait();

                int i1 = i;
                int j1 = j;

                if (_flagCount[i1] > 2 || _flagCount[j1] > 2) {
                    semaphore.Release();
                    continue;
                }

                var task = Task.Run(() => {
                    try {
                        var bots = new List<IBot>();
                        var botIdToAgent = new Dictionary<int, int>();

                        bots.Add(new Bot(0, _population[i1], $"Run_{i1}_Bot_{i1}"));
                        bots.Add(new Bot(1, _population[j1], $"Run_{i1}_Bot_{j1}"));

                        botIdToAgent[0] = i1;
                        botIdToAgent[1] = j1;

                        lock (fitnessLock) {
                            _population[i1].Played++;
                            _population[j1].Played++;
                        }
                        
                        if (_flagCount[i1] > 2 || _flagCount[j1] > 2) {
                            semaphore.Release();
                            return;
                        }

                        var result = PlayGameWithBots(bots, playerCount);

                        bool killedGame = _wasGameKilled;
                        _wasGameKilled = false;

                        foreach (var bot in bots) {
                            lock (fitnessLock) {
                                bot.Result(result);
                                if (bot is Bot botBot) {
                                    int agentIndex = botIdToAgent[bot.Id];
                                    _population[agentIndex] = botBot.GetAgent;

                                    if (killedGame) {
                                        _flagCount[agentIndex]++;
                                        Console.WriteLine($"Agent {agentIndex} flags: {_flagCount[agentIndex]}");
                                    }
                                }
                            }
                        }
                    }
                    finally {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }
        }
        Task.WaitAll(tasks.ToArray());
        var topAgents = _population.OrderByDescending(a => (a.PointsPerGame() * a.WinRate())).Take(_population.Count / 4).ToList();

        for (int i = 0; i < topAgents.Count; i++) {
            Console.WriteLine($"Played: {topAgents[i].Played} Wins: {topAgents[i].Wins} Points average: {topAgents[i].PointsPerGame()}");
            topAgents[i].PrintWeights();
            topAgents[i].ResetValues();
        }


        _population = BreedNewGeneration(topAgents);
    }
    

    private List<Agent> BreedNewGeneration(List<Agent> parents) {
        var newGeneration = new List<Agent>();
        int minPopulationSize = 32;
        newGeneration.AddRange(parents);
        int populationCount = _population.Count / 2;
        if (populationCount < minPopulationSize) {
            populationCount = minPopulationSize;
        }
        while (newGeneration.Count < populationCount) {
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
            childWeights[rule] = (parent1.RuleWeights[rule] + parent2.RuleWeights[rule]) / 2;
        return childWeights;
    }

    private void Mutate(Dictionary<string, double> weights) {
        foreach (var rule in weights.Keys.ToList())
            if (_random.NextDouble() < 0.2) // 20% mutation chance
                weights[rule] += (float) (_random.NextDouble() * 0.4 - 0.2); // Small perturbation
    }
    
    private Dictionary<int, int> PlayGameWithBots(List<IBot> bots, int playerCount) {
        var localBots = bots;
        string[] names = bots.Select((b, i) => $"Bot_{b.Id}").ToArray();
        bool mode = false;

        void OnNextTakingTurn(object sender, MyEventArgs e) {
            var game = e.Board;

            if (game.Round > 50) {
                Console.WriteLine("Killing long game");
                _wasGameKilled = true;
                game.FinishGame();
                return;
            }
            
            int curr = game.CurrentPlayer;
            var botMove = localBots[curr].DoMove(game);
            int[] action = StringArrToIntArr(botMove.Split(' '));
            game.EventManager.QueueEvent(() => game.Move(action[0], action[1], action[2]));
        }

        void OnNextPlacingTurn(object sender, MyEventArgs e){
            var game = e.Board;
            int curr = game.CurrentPlayer;

            if (!game.IsAdvanced) {
                game.EventManager.QueueEvent(() => game.Calculate());
            } else {
                game.EventManager.QueueEvent(() => game.Calculate(int.Parse(localBots[curr].Place(game))));
            }
        }

        Board game = new Board(playerCount, names, mode); // no logging for parallel
        game.NextTakingMove += OnNextTakingTurn;
        game.NextPlacingMove += OnNextPlacingTurn;

        game.StartGame();
        while (game.Phase != Phase.GameOver) ; // Wait till game ends

        var scores = new Dictionary<int, int>();
        for (int i = 0; i < game.Players.Length; i++) {
            scores[i] = game.Players[i].pointCount;
            Console.WriteLine($"{i + 1}. {game.Players[i].name}: {scores[i]}");
        }
        return scores;
    }
    
    private static int[] StringArrToIntArr(string[] arr) {
        List<int> output = new List<int>();

        foreach (var val in arr) {
            output.Add(int.Parse(val));
        }

        return output.ToArray();
    }

}