using System;
using System.Collections.Generic;
using Azul;
using Random = System.Random;

namespace NonML {

   
    public class RandomBot : IBot {
        private Random random;
        private int _id;

        public RandomBot(int id) {
            random = new Random();
            _id = id;
        }

        public string DoMove(Azul.Board board) {

            var possibleMoves = board.GetValidMoves();
            
            Move option = possibleMoves[random.Next(possibleMoves.Length)];
            
            return $"{option.plateId} {option.tileId} {option.bufferId}";

        }

        public string Place(Azul.Board board) {
            Player me = board.Players[_id];
            var row = me.FullBuffers()[0];
            List<int> possiblePositions = new List<int>();
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if (PossibleCol(me.wall, row, i, me.GetBufferData(row).id)) {
                    possiblePositions.Add(i);
                }
            }

            return $"{possiblePositions[random.Next(possiblePositions.Count)]}";
        }

        public int GetId() => _id;
        
        public void Result(Dictionary<int,int> result) {}

        private bool PossibleCol(int[,] wall, int row, int column, int chosenType) {
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if (wall[i, column] == chosenType) {
                    return false;
                }
            }

            return wall[row, column] == Globals.EMPTY_CELL;
        }
    }
}