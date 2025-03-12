using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;
using Azul;
using DeepQLearningBot;


namespace AzulTrainer;

public class Options {
    [Option('m', "mode", Required = false, Default = 0, HelpText = "Game mode (Advanced = 1, Basic = 0) ")]
    public int Mode { get; set; } = 0;

    [Option('l', "list-of-players", Required = false, Default = "deep rand", HelpText = "list of which player is which")]
    public string ListOfIncoming { get; set; } = "deep rand";
    
    [Option('o', "output-file", Required = false, Default = "azul_log.txt", HelpText = "log file")]
    public string logFile { get; set; } = "log.txt";
    
}

public class Trainer {
    const int BALANCER = 10;
    private static IBot[] bots;
    
    private const string networkFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/network.json";

    
    public static void Main(string[] args) {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {
            string[] botNames = o.ListOfIncoming.Split(' ');
            int count = botNames.Length;
            string[] names = new string[count];
            bots = new IBot[count];
            for (int i = 0; i < count; i++) {
                string type = botNames[i];
                if(type == "random") bots[i] = new randomBot.Bot(i);
                else bots[i] = BotFactory.CreateBot(type, i);
                names[i] = botNames[i] + i;
            }

            int lastWinner = 0;
            while(!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)) {
                string logFile = logFileName(o.logFile + "/log");
                lastWinner = playGame(count, names, o.Mode == 1, logFile);
                if (bots[lastWinner] is IgnoringBot ignoringBot) {
                    NeuralNetwork nc;
                    nc = ignoringBot.GetNetwork();
                    //SaveSystem.JsonSaver.Save(nc,networkFile);
                    Console.WriteLine("Updateting network...");
                    foreach (var bot in bots) {
                        if (bot is IgnoringBot iBot) {
                            iBot.LoadNetwork(nc);
                        }
                    }
                }
            }
            if (bots[lastWinner] is IgnoringBot ignorantBot) {
                ignorantBot.saveThis = true;
            }

        });
    }

    private static int playGame(int count, string[] names, bool mode, string logFile) {
        Board game = new Board(count, names, mode, logFile);
        game.NextTakingMove += OnNextTakingTurn;
        game.NextPlacingMove += OnNextPlacingTurn;

        Console.WriteLine("Game started");
        game.StartGame();
            
        while (game.Phase != Phase.GameOver) Thread.Sleep(5);//ish secure 

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

        return index;
    }

    private static void OnNextTakingTurn(object sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        var botMove = bots[curr].DoMove(game);
        int[] action = StringArrToIntArr(botMove.Split(' '));
        Console.WriteLine($"Player {player.name} : action {action[0]}, {action[1]}, {action[2]}");
        game.Move(action[0], action[1], action[2]);
    }

    private static void OnNextPlacingTurn(object sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {player.name} : placing");
        
        if (!game.isAdvanced) {
            game.Calculate();
        }
        else {
            game.Calculate(int.Parse(bots[curr].Place(game)));
        }
    }
    
    private static int[] StringArrToIntArr(string[] arr) {
        List<int> output = new List<int>();

        foreach (var val in arr) {
            output.Add(int.Parse(val));
        }

        return output.ToArray();
    }
    
    private static string logFileName(string baseName) {
        string fileName = baseName + ".txt";
        long index = 0;
        while (File.Exists(fileName)) {
            fileName = $"{baseName}_{index++}.txt";
        }
        return fileName;
    }
}