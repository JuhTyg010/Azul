using CommandLine;
using Azul;
using DeepQLearningBot;

namespace AzulTrainer;

public class Options {
    [Option('m', "mode", Required = false, Default = 0, HelpText = "Game mode (Advanced = 1, Basic = 0) ")]
    public int Mode { get; set; } = 0;

    [Option('l', "list-of-players", Required = false, Default = "deep rand", HelpText = "list of which player is which")]
    public string ListOfIncoming { get; set; } = "deep rand";
    
    [Option('d', "working-dir", Required = false, Default = "/home/", HelpText = "working directory to save files")]
    public string WorkingDir { get; set; } = "/home/";
    
    [Option('r', "reward-type", Required = true, Default = 0, HelpText = "id of the specific reward function")]
    public int RewardType { get; set; } = 0;
    
    [Option('c', "count", Required = true, Default = 100_000, HelpText = "num of iterations to run")]
    public int Count { get; set; } = 100_000;
    
}

public class Trainer {
    
    private const string LogDir = "Logs";
    private const string ScoreFileName = "score.txt";
    
    const int Balancer = 10;
    private static IBot[] _bots = null!;
    
    private static string _scorePath = "score.txt";
    
    public static void Main(string[] args) {
        //PPO.Trainer.Run();

        Parser.Default.ParseArguments<Options>(args).WithParsed(o => {
            Console.WriteLine("Current working directory: " + Environment.CurrentDirectory);

            string[] botNames = o.ListOfIncoming.Split(' ');
            int count = botNames.Length;
            string[] names = new string[count];
            Console.WriteLine($"Count {count}, bot names:");
            foreach (var name in botNames) Console.WriteLine($"\t{name}");
            _scorePath = PathCombiner(o.WorkingDir, ScoreFileName);
            _bots = new IBot[count];
            for (int i = 0; i < count; i++) {
                string type = botNames[i];
                if (NonML.BotFactory.WasRecognised(type)) {
                    _bots[i] = NonML.BotFactory.CreateBot(type, i, o.WorkingDir);
                    Console.WriteLine("NonML Bot: " + type);
                }
                else if (type == "PPO") {
                    Console.WriteLine("PPO Bot: " + type);
                    _bots[i] = new PPO.Bot(i, o.RewardType, o.WorkingDir);
                }
                else if(type == "ignoring") _bots[i] = new IgnoringBot(i, o.RewardType, o.WorkingDir);
                names[i] = botNames[i] + i;
            }
            //!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
            int runIter = 0;
            while(runIter < o.Count) {
                runIter++;
                string logPath = PathCombiner(o.WorkingDir, LogDir);

                if (!Path.Exists(logPath)) Directory.CreateDirectory(logPath);
                
                string logFile = LogFileName(logPath + "/log");
                var scores = PlayGame(count, names, o.Mode == 1, logFile);
                foreach (var bot in _bots) {
                    bot.Result(scores);
                }
            }
        });//*/


    }

    private static Dictionary<int,int> PlayGame(int count, string[] names, bool mode, string logFile) {
        Board game = new Board(count, names, mode, logFile);
        game.NextTakingMove += OnNextTakingTurn!;
        game.NextPlacingMove += OnNextPlacingTurn!;

        Console.WriteLine("Game started");
        game.StartGame();
            
        while (game.Phase != Phase.GameOver); 

        Console.WriteLine("Game over");
        Player[] players = game.Players.ToArray();
        int maxScore = Int32.MinValue;
        int index = 0;
        for (int i = 0; i < players.Length; i++) {
            if (players[i].pointCount > maxScore) {
                maxScore = players[i].pointCount;
                index = i;
            }
        }
        Array.Sort(players, (a, b) => a.pointCount > b.pointCount ? -1 : 1);
        for (int i = 0; i < players.Length; i++) {
            Console.WriteLine($" {i + 1}.: {players[i].name} : points {players[i].pointCount}");
        }
        string score = "";
        foreach (var player in game.Players) score += $"{player.pointCount} ";
        File.AppendAllText(_scorePath, score + Environment.NewLine);
        
        Dictionary<int,int> playerScores = new Dictionary<int,int>();
        for (int i = 0; i < players.Length; i++) {
            playerScores.Add(i, players[i].pointCount);
        }
        

        return playerScores;
    }

    private static void OnNextTakingTurn(object sender, MyEventArgs e) {
        var game = e.Board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {curr}");
        var botMove = _bots[curr].DoMove(game);
        int[] action = StringArrToIntArr(botMove.Split(' '));
        Console.WriteLine($"Player {player.name} : action {action[0]}, {action[1]}, {action[2]}");
        game.Move(action[0], action[1], action[2]);
    }

    private static void OnNextPlacingTurn(object sender, MyEventArgs e) {
        var game = e.Board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {player.name} : placing");
        
        if (!game.IsAdvanced) {
            game.Calculate();
        }
        else {
            game.Calculate(int.Parse(_bots[curr].Place(game)));
        }
    }
    
    private static int[] StringArrToIntArr(string[] arr) {
        List<int> output = new List<int>();

        foreach (var val in arr) {
            output.Add(int.Parse(val));
        }

        return output.ToArray();
    }
    
    private static string LogFileName(string baseName) {
        string fileName = baseName + ".txt";
        long index = 0;
        while (File.Exists(fileName)) {
            fileName = $"{baseName}_{index++}.txt";
        }
        return fileName;
    }

    private static string PathCombiner(string baseName, string fileName) {
        if (baseName[^1] != '/') baseName += '/';
        return baseName + fileName;
    }
}