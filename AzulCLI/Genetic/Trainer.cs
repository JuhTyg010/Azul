using Azul;
using NonML;

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

    public void RunGeneration(int playerCount) {
        int gamesToPlay = 4 * _population.Count; // Total number of games to simulate
        int agentsPerGame = playerCount;

        var rnd = new Random();
        var tasks = new List<Task>();

        object fitnessLock = new object(); // Lock for updating fitness

        for (int i = 0; i < gamesToPlay; i++) {
            tasks.Add(Task.Run(() => {
                var bots = new List<IBot>();
                var botIdToAgent = new Dictionary<int, int>();

                for (int j = 0; j < agentsPerGame; j++) {
                    int id = rnd.Next(_population.Count);
                    var bot = new Bot(j, _population[id], $"Run_{i}_Bot_{id}");
                    bots.Add(bot);
                    botIdToAgent[j] = id;
                    _population[id].Played++;
                }

                //bots.Add(new HeuristicBot(agentsPerGame));

                var result = PlayGameWithBots(bots, playerCount);

                foreach (var bot in bots) {
                    lock (fitnessLock) {
                        bot.Result(result);
                        if (bot is Bot) {
                            var botBot = (Bot) bot;
                            _population[botIdToAgent[bot.Id]] = botBot.GetAgent;
                        }
                    }
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        var topAgents = _population.OrderByDescending(a => a.WinRate()).Take(_population.Count / 5).ToList();

        for (int i = 0; i < topAgents.Count; i++) {
            Console.WriteLine($"Played: {topAgents[i].Played} Wins: {topAgents[i].Wins}");
            topAgents[i].PrintWeights();
            topAgents[i].ResetValues();
        }


        _population = BreedNewGeneration(topAgents);
    }
    

    private List<Agent> BreedNewGeneration(List<Agent> parents) {
        var newGeneration = new List<Agent>();

        newGeneration.AddRange(parents);
        
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
            childWeights[rule] = (parent1.RuleWeights[rule] + parent2.RuleWeights[rule]) / 2;
        return childWeights;
    }

    private void Mutate(Dictionary<string, double> weights) {
        foreach (var rule in weights.Keys.ToList())
            if (_random.NextDouble() < 0.1) // 10% mutation chance
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