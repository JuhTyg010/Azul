using Azul;
using System;
using System.Linq;

public static class Writer {

    private static readonly char EmptyPlace = '_';
    public static void PrintBoard(Player[] otherPlayers, Plate[] plates, CenterPlate centerPlate, Player currentPlayer) {
        Console.WriteLine("Others:");
        for (int i = 0; i < otherPlayers.Length; i++) {
            Console.Write(" ");
            PrintPlayer(otherPlayers[i]);
        }
        Console.WriteLine();
        
        PrintTable(plates, centerPlate);
        Console.WriteLine();
        Console.WriteLine($"Me: {currentPlayer.name}");
        Console.WriteLine($" Score: {currentPlayer.pointCount}");
        Console.Write(" Data:  ");
        for (int i = 0; i < 5; i++) {
            PrintPlayerRow(currentPlayer.GetBufferData(i), i + 1,
                Enumerable.Range(0,currentPlayer.wall.GetLength(0))
                    .Select(j => currentPlayer.wall[i,j]).ToArray());
            Console.Write(" ");
        }
        
        PrintPlayerFloor(currentPlayer);
        Console.WriteLine();
        
        Console.Write("Action: ");
        
    }

    private static void PrintTable(Plate[] plates, CenterPlate center) {
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
        firstInfo += center.isFirst ? "false" : "true";
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
                    .Select(j => player.wall[i,j]).ToArray());
            Console.Write(" ");
        }

        PrintPlayerFloor(player);
        Console.WriteLine();
        
    }

    private static void PrintPlayerRow(Tile buffer, int bufferSize, int[] wallRow) {
        string buff = "";
        if (buffer.Id == Globals.EmptyCell) {
            for (int i = 0; i < bufferSize; i++) {
                buff += EmptyPlace;
            }
        } else { 
            buff = new string(buffer.Id.ToString()[0], buffer.Count);
            for (int i = buffer.Count; i < bufferSize; i++) {
                buff += EmptyPlace;
            }
        }

        string wall = "";
        foreach (var val in wallRow) {

            if (val == Globals.EmptyCell) {
                wall += EmptyPlace;
            } else if (val == Globals.First) {
                wall += "f";
            } else {
                wall += val.ToString();
            }
            
        }
        
        Console.Write($"{buff} -> {wall}");
    }

    private static void PrintPlayerFloor(Player player) {
        for (int i = 0; i < Globals.FloorSize; i++) {
            if (i >= player.floor.Count) {
                Console.Write(EmptyPlace);
            } else {
                if(player.floor[i] == Globals.First) Console.Write("F");
                else Console.Write(player.floor[i]);
            }
        }
    }
    
    
}