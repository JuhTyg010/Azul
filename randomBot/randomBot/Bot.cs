using System.Data;
using Azul;

namespace randomBot;

public class Bot {
    private Random random;
    public int id { get; private set; }
    
    public Bot(int _id) {
        random = new Random();
        id = _id;
    }

    public string DoMove(Board board) {
        Player me = board.Players[id];
        List<int> posibleBuffers = new List<int>();
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            var data = me.GetBufferData(i);
            if (data.count < i + 1) {
                posibleBuffers.Add(i);
            }
        }
        int chosenBuffer = posibleBuffers[random.Next(posibleBuffers.Count)];
        int chosenType = -1;    //not chosen yet
        int chosenPlate = -1;   //not chosen yet
        if (me.GetBufferData(chosenBuffer).id == Globals.EMPTY_CELL) {
            //choose any plate and non 0 value from it
            List<int> possiblePlates = new List<int>();
            for(int i = 0; i < board.Plates.Length; i++) {
                if (!board.Plates[i].isEmpty) {
                    possiblePlates.Add(i);
                }
            }
            if(!board.Center.isEmpty) possiblePlates.Add(board.Plates.Length);
            
            chosenPlate = possiblePlates[random.Next(possiblePlates.Count)];
            if (chosenPlate == board.Plates.Length) {
                var possible = board.Center.GetCounts();
                List<int>possibleTypes = new List<int>();
                for (int i = 0; i < possible.Length; i++) {
                    if (possible[i].count != 0) {
                        possibleTypes.Add(i);
                    }
                }
                chosenType = possibleTypes[random.Next(possibleTypes.Count)];
            }
            else {
                var possible = board.Plates[chosenPlate].GetCounts();
                List<int>possibleTypes = new List<int>();
                for (int i = 0; i < possible.Length; i++) {
                    if (possible[i].count != 0) {
                        possibleTypes.Add(i);
                    }
                }
                chosenType = possibleTypes[random.Next(possibleTypes.Count)];
            }

        }
        else {
            chosenType = me.GetBufferData(chosenBuffer).id;
            List<int> possiblePlates = new List<int>();
            for (int i = 0; i < board.Plates.Length; i++) {
                if (!board.Plates[i].isEmpty && board.Plates[i].TileCountOfType(chosenType) != 0) {
                    possiblePlates.Add(i);
                }
            }
            if(!board.Center.isEmpty && board.Center.TileCountOfType(chosenType) != 0) 
                possiblePlates.Add(board.Plates.Length);
            chosenPlate = possiblePlates[random.Next(possiblePlates.Count)];
        }

        return $"{chosenPlate} {chosenType} {chosenBuffer}";
    }

    public string Place(Board board) {
        Player me = board.Players[id];
        var row = me.FullBuffers()[0];
        List<int> possiblePositions = new List<int>();
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            if (possibleCol(me.wall, row, i, me.GetBufferData(row).id)) {
                possiblePositions.Add(i);
            }
        }
        return $"{possiblePositions[random.Next(possiblePositions.Count)]}";
    }

    private bool possibleCol(int[,] wall, int row, int column, int chosenType) {
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            if (wall[i, column] == chosenType) {
                return false;
            }
        }
        return wall[row, column] == Globals.EMPTY_CELL;
    }
}