using System.Diagnostics.CodeAnalysis;
using Azul;

namespace Genetic {

    public class Rules {
        [Rule]
        public static bool CompletesRow(Board board, Move move) {
            if (move.BufferId == Globals.BufferCount) return false;
            var wall = board.Players[board.CurrentPlayer].wall;
            int emptyInRow = Enumerable.Range(0, Globals.WallDimension)
                .Count(col => wall[move.BufferId, col] == Globals.EmptyCell);
            return emptyInRow == 1; //one to being filled
        }
        
        public static bool BlocksOpponent(Board board, Move move) {
            
            return false; //TODO: implement this
        }
        
        
        [Rule]
        public static bool CompletesBuffer(Board board, Move move) {
            if (move.BufferId == Globals.BufferCount) return false;

            var player = board.Players[board.CurrentPlayer];
            int count = move.PlateId != board.Plates.Length
                ? board.Plates[move.PlateId].TileCountOfType(move.TileId)
                : board.Center.TileCountOfType(move.TileId);
            var buffer = player.GetBufferData(move.BufferId);
            if (buffer.Count + count >= buffer.Id + 1) {
                return true;
            }
            return false;
        }

        [Rule]
        public static bool IWillStart(Board board, Move move) {
            return move.PlateId == board.Plates.Length && board.Center.isFirst;
        }

        [Rule]
        public static bool SomethingGoesToFloor(Board board, Move move) {
            if (move.BufferId == Globals.BufferCount) return true;

            var player = board.Players[board.CurrentPlayer];
            int count = move.PlateId != board.Plates.Length
                ? board.Plates[move.PlateId].TileCountOfType(move.TileId) 
                : board.Center.TileCountOfType(move.TileId);
            var buffer = player.GetBufferData(move.BufferId);
            if (buffer.Count + count > buffer.Id + 1) {
                return true;
            }
            return IWillStart(board, move);
        }

        [Rule]
        public static bool CompletesColumn(Board board, Move move) {
            if(move.BufferId == Globals.BufferCount) return false;
            int col = Board.FindColInRow(move.BufferId, move.TileId);
            var wall = board.Players[board.CurrentPlayer].wall;
            int emptyInCol = Enumerable.Range(0, Globals.WallDimension)
                .Count(row => wall[row, col] == Globals.EmptyCell);
            return emptyInCol == 1;
        }

        [Rule]
        public static bool MultipleInRow(Board board, Move move) {
            if (move.BufferId == Globals.BufferCount) return false;
            int col = Board.FindColInRow(move.BufferId, move.TileId);
            var wall = board.Players[board.CurrentPlayer].wall;
            int colMinusOne = Math.Max(0, col - 1);
            int colPlusOne = Math.Min(Globals.WallDimension - 1, col + 1);
            if (col != colMinusOne && wall[move.BufferId, colMinusOne] != Globals.EmptyCell) {
                return true;
            }
            if (col != colPlusOne && wall[move.BufferId, colPlusOne] != Globals.EmptyCell) {
                return true;
            }

            return false;
        }

        [Rule]
        public static bool MultipleInColumn(Board board, Move move) {
            if (move.BufferId == Globals.BufferCount) return false;
            int col = Board.FindColInRow(move.BufferId, move.TileId);
            var wall = board.Players[board.CurrentPlayer].wall;
            int rowMinusOne = Math.Max(0, move.BufferId - 1);
            int rowPlusOne = Math.Min(Globals.WallDimension - 1, move.BufferId + 1);
            if (move.BufferId != rowMinusOne && wall[rowMinusOne, col] != Globals.EmptyCell) {
                return true;
            }
            if (move.BufferId != rowPlusOne && wall[rowPlusOne, col] != Globals.EmptyCell) {
                return true;
            }
            return false;
        }

        [Rule]
        public static bool AllOfOneColor(Board board, Move move) {
            var wall = board.Players[board.CurrentPlayer].wall;
            int tilesOfType = Enumerable.Range(0, wall.GetLength(0))
                .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                    .Select(column => wall[row, column])).Count(value => value == move.TileId);
            return tilesOfType == Globals.TypeCount - 1;
        }

        [Rule]
        public static bool TakenMaxCount(Board board, Move move) {
            int count = move.PlateId != board.Plates.Length
                ? board.Plates[move.PlateId].TileCountOfType(move.TileId) 
                : board.Center.TileCountOfType(move.TileId);
            int maxVal = 0;
            int tempCount = 0;
            foreach (var plate in board.Plates) {
                tempCount = plate.GetCounts().Max(p => p.Count);
                if (tempCount > maxVal) {
                    maxVal = tempCount;
                }
            }
            tempCount = board.Center.GetCounts().Max(p => p.Count);
            if (tempCount > maxVal) {
                maxVal = tempCount;
            }

            return maxVal == count;
        }
        
        
    }
}