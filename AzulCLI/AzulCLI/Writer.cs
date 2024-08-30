using System.Linq;
using System.Security.Cryptography;

namespace AzulCLI;

public static class Writer {

    private static readonly char EmptyPlace = '_';
    public static void PrintBoard(Board game) {
        Console.WriteLine("Others:");
        for (int i = 0; i < game.Players.Length; i++) {
            if (i == game.CurrentPlayer) continue;
            
            Console.Write(" ");
            PrintPlayer(game.Players[i]);
        }
        Console.WriteLine();
        
        PrintTable(game.Plates, game.Center, game.fisrtTaken);
        Console.WriteLine();

        Player currentPlayer = game.Players[game.CurrentPlayer];
        Console.WriteLine($"Me: {currentPlayer.name}");
        Console.WriteLine($" Score: {currentPlayer.pointCount}");
        Console.Write(" Data:  ");
        for (int i = 0; i < 5; i++) {
            PrintPlayerRow(currentPlayer.GetBufferData(i), i + 1,
                Enumerable.Range(0,currentPlayer.wall.GetLength(0))
                    .Select(j => currentPlayer.wall[j,i]).ToArray());
            Console.Write(" ");
        }
        
        for (int i = 0; i < currentPlayer.FloorSize(); i++) {
            if (i >= currentPlayer.floor.Count) {
                Console.Write(EmptyPlace);
            } else {
                Console.Write(currentPlayer.floor[i]);
            }
        }
        Console.WriteLine();
        
        Console.Write("Action: ");
        
    }

    private static void PrintTable(Plate[] plates, CenterPlate center, bool firstTaken) {
        int plateCount = plates.Length;
        Console.Write($"Table:   center");
        for (int i = 0; i < plateCount; i++) {
            Console.Write($"| Hold{i}");
        }
        Console.WriteLine();

        for (int i = 0; i < 5; i++) {
            Console.Write(" ");
            PrintTypeData(plates, center, i);
            Console.WriteLine();
        }

        string firstInfo = "First center taken: ";
        firstInfo += firstTaken ? "true" : "false";
        Console.WriteLine($" {firstInfo}");
    }

    private static void PrintTypeData(Plate[] plates, CenterPlate center, int typeId) {
        Console.Write($"Type {typeId}: ");
        
        Console.Write($"   {center.TileCountOfType(typeId)}");
        if(center.TileCountOfType(typeId) < 10) Console.Write(" ");
        Console.Write(" ");

        for (int i = 0; i < plates.Length; i++) {
            Console.Write($"|   {plates[i].TileCountOfType(typeId)}  ");
        }
    }
    
    private static void PrintPlayer(Player player) {
        Tile[] buffers = new Tile[5];
        for (int i = 0; i < 5; i++) {
            buffers[i] = player.GetBufferData(i);
        }
        Console.Write(player.name + ": ");
        Console.Write(player.pointCount + " ");
        for (int i = 0; i < 5; i++) {
            PrintPlayerRow(buffers[i], i + 1, 
                Enumerable.Range(0,player.wall.GetLength(0))
                    .Select(j => player.wall[j,i]).ToArray());
            Console.Write(" ");
        }

        for (int i = 0; i < player.FloorSize(); i++) {
            if (i >= player.floor.Count) {
                Console.Write(EmptyPlace);
            } else {
                Console.Write(player.floor[i]);
            }
        }
        Console.WriteLine();
        
    }

    private static void PrintPlayerRow(Tile buffer, int bufferSize, int[] wallRow) {
        string buff = "";
        if (buffer.id == Globals.EMPTY_CELL) {
            for (int i = 0; i < bufferSize; i++) {
                buff += EmptyPlace;
            }
        } else { 
            buff = new string(buffer.id.ToString()[0], buffer.count);
            for (int i = buffer.count; i < bufferSize; i++) {
                buff += EmptyPlace;
            }
        }

        string wall = "";
        foreach (var val in wallRow) {

            if (val == Globals.EMPTY_CELL) {
                wall += EmptyPlace;
            } else if (val == Globals.FIRST) {
                wall += "f";
            } else {
                wall += val.ToString();
            }
            
        }
        
        Console.Write($"{buff} -> {wall}");
    }
    
    
}