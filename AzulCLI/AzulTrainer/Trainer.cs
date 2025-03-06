using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;
using Azul;


namespace AzulTrainer;

public class Options {
    [Option('m', "mode", Required = false, Default = 0, HelpText = "Game mode (Advanced = 1, Basic = 0) ")]
    public int Mode { get; set; } = 0;

    [Option('l', "list-of-players", Required = false, Default = "deep rand", HelpText = "list of which player is which")]
    public string ListOfIncoming { get; set; } = "deep rand";
    
}

public class Trainer {
    private static IBot[] bots;
    public static void Main(string[] args) {
        /*Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {
            string[] botNames = o.ListOfIncoming.Split(' ');
            int count = botNames.Length;
            string[] names = new string[count];
            bots = new IBot[count];
            for (int i = 0; i < count; i++) {
                switch (botNames[i]) {
                    case "deep": bots[i] = new DeepQLearningBot.Bot(i);
                        break;
                    case "rand": bots[i] = new randomBot.Bot(i);
                        break;
                    default: throw new DataException($"Unknown bot: {botNames[i]}");
                } 
                names[i] = botNames[i] + i;
            }

            Board game = new Board(count, names, o.Mode == 1);
            game.NextTakingMove += OnNextTakingTurn;
            game.NextPlacingMove += OnNextPlacingTurn;

            Console.WriteLine("Game started");
            game.StartGame();
            
            while (game.Phase != Phase.GameOver) Thread.Sleep(5);//ish secure 

            Console.WriteLine("Game over");
            Player[] players = game.Players.ToArray();
            Array.Sort(players, (a, b) => a.pointCount > b.pointCount ? -1 : 1);
            for (int i = 0; i < players.Length; i++) {
                Console.WriteLine($" {i + 1}.: {players[i].name} : points {players[i].pointCount}");
            }

        });*/
            
        Console.WriteLine("Welcome to Azul Trainer!");
        while (true) {
            Console.WriteLine("running game");
            
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) {
                Console.WriteLine("Exit condition met. Stopping process execution.");
                break;
            }
            Process p = new Process();
            p.StartInfo.FileName = "/home/juhtyg/Desktop/Azul/AzulCLI/AzulCLI/bin/Debug/net8.0/AzulCLI"; // Need full path of application
            p.StartInfo.Arguments = "-m 0 -l \"B_random_a B_ignoring_b\"";
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();
        }
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
}