using System.Data;
using Azul;
using DeepQLearningBot;
using CommandLine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using randomBot;
using Bot = randomBot.Bot;

namespace AzulCLI;

public class Options
{
    [Option('m', "mode", Required = false, Default = 0, HelpText = "Game mode (Advanced = 1, Basic = 0) ")]
    public int Mode { get; set; } = 0;

    [Option('l', "list-of-players", Required = false, Default = "H_a H_b", HelpText = "list of which player is which")]
    public string ListOfIncoming { get; set; } = "human human";
    
    [Option('v', "verbose", Required = false, Default = false, HelpText = "Disable verbose logging"), ]
    public bool Verbose { get; set; }
    
    [Option('d', "log-file", Required = false, Default = "", HelpText = "directory where to store log file")]
    public string logFile { get; set; } = "";
}

public class CLIGameManager {

    private static List<IBot> botPlayers;
    private static int[] botIds;
    private static bool printTable;
    
    static void Main(string[] args) {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
        {
            Console.WriteLine($"You choose a mode to {o.Mode} ");
            bool isAdvanced = o.Mode == 1;
            Console.WriteLine($" You choose game for {o.ListOfIncoming.Length} players");
            string[] playerSetup = o.ListOfIncoming.Split(" ");
            
            botPlayers = new List<IBot>();
            for (int i = 0; i < playerSetup.Length; i++) {
                if (playerSetup[i].Split("_")[0] == "B") {
                    string type = playerSetup[i].Split("_")[1];
                    if(type == "random") botPlayers.Add(new randomBot.Bot(i));
                    botPlayers.Add(BotFactory.CreateBot(type, i));
                }
            }
            botIds = new int[botPlayers.Count];
                
            for (int i = 0; i < botPlayers.Count; i++){
                botIds[i] = botPlayers[i].GetId();
            }
                
            string[] names = new string[playerSetup.Length];
            for (int i = 0; i < playerSetup.Length; i++) {
                names[i] = playerSetup[i];
            }

            printTable = !o.Verbose;

            string logFile = logFileName(o.logFile + "/log");
            
            Board game = new Board(playerSetup.Length, names, isAdvanced, logFile);
            game.NextTakingMove += OnNextTakingTurn;
            game.NextPlacingMove += OnNextPlacingTurn;
            
            game.StartGame();

            while (game.Phase != Phase.GameOver);// Thread.Sleep(20);//ish secure 

            Console.WriteLine("Game over");
            
            Player[] players = game.Players.ToArray();
            Array.Sort(players, (a, b) => a.pointCount > b.pointCount ? -1 : 1);
            Dictionary<int,int> results = new Dictionary<int,int>();
            for (int i = 0; i < players.Length; i++) {
                results[i] = players[i].pointCount;
                Console.WriteLine($" {i + 1}.: {players[i].name} : points {players[i].pointCount}");
            }

            foreach (var bot in botPlayers) {
                bot.Result(results);
            }
            
            saveResult(results);
            /*for (int i = 0; i < game.Players.Length; i++) {
                Console.WriteLine($"Player {game.Players[i].name}: {game.Players[i].pointCount}");
            }*/
        });
    }

    private static void OnNextTakingTurn(object? sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        
        Console.WriteLine($" Next turn {game.Players[curr].name} (press enter to go)");
        
        if (botIds.Contains(curr)) {
            if(printTable)
                Writer.PrintBoard(game.Players.Where(x => x != game.Players[curr]).ToArray(), 
                game.Plates, game.Center, game.Players[curr]);
            
            string botMove = botPlayers[FindBot(curr, botPlayers)].DoMove(game);
            Console.WriteLine(botMove);
            int[] action = StringArrToIntArr(botMove.Split(' '));
            game.Move(action[0], action[1], action[2]); //bot cant do illegal move (for now)
        }
        else {
            Console.ReadLine();
            
            if(printTable)
                Writer.PrintBoard(game.Players.Where(x => x != game.Players[curr]).ToArray(), 
                game.Plates, game.Center, game.Players[curr]);  
            
            
            string? input = Console.ReadLine();
            if(input == null) throw new NoNullAllowedException("No input");
            int[] action = StringArrToIntArr(input.Split());
            // <{0-9}> <{0-4}> <{0-5}> first is which plate (last is center) second is type and last is buffer id
            bool moveDone = game.Move(action[0], action[1], action[2]);
            while (!moveDone) {
                Console.WriteLine("Invalid move try again");
                
                if(printTable)
                    Writer.PrintBoard(game.Players.Where(x => x != game.Players[curr]).ToArray(), 
                    game.Plates, game.Center, game.Players[curr]);  
                
                action = StringArrToIntArr(Console.ReadLine().Split());
                moveDone = game.Move(action[0], action[1], action[2]);
            }
        }
    }

    private static void OnNextPlacingTurn(object? sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        
        if(printTable)
            Writer.PrintBoard(game.Players.Where(x => x != game.Players[curr]).ToArray(), 
            game.Plates, game.Center, game.Players[curr]);
        
        Console.WriteLine($" Next turn in filling {game.Players[curr].name} (press enter to go)");
        if(!botIds.Contains(curr)) Console.ReadLine();
        if (!game.isAdvanced) {
            game.Calculate();
        }
        else {
            if (botIds.Contains(game.CurrentPlayer)) {
                game.Calculate(int.Parse(botPlayers[FindBot(curr, botPlayers)].Place(game)));
            }
            else {
                string? input = Console.ReadLine(); //should be {0-4} representing the column of first buffer
                if(input == null) throw new NoNullAllowedException("No input");
                game.Calculate(int.Parse(input));
            }
        }
    }

    private static int[] StringArrToIntArr(string[] arr) {
        List<int> output = new List<int>();

        foreach (var val in arr) {
            output.Add(int.Parse(val));
        }

        return output.ToArray();
    }

    private static int FindBot(int botId, List<IBot> botPlayers) {
        for (int i = 0; i < botPlayers.Count; i++) {
            
            if (botPlayers[i].GetId() == botId) {
                return i;
            }
        }

        return -1;
    }
    
    private static string logFileName(string baseName) {
        string fileName = baseName + ".txt";
        long index = 0;
        while (File.Exists(fileName)) {
            fileName = $"{baseName}_{index++}.txt";
        }
        return fileName;
    }

    private static void saveResult(Dictionary<int, int> results) {
        string fileName = "/home/juhtyg/Desktop/Azul/results.json";
        var ser = JsonSerializer.Serialize<Dictionary<int, int>>(results);
        File.AppendAllText(fileName, ser);
    }
}