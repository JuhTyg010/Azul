using System;
using System.Collections.Generic;
using Azul;
using Random = System.Random;

namespace NonML {

   
    public class RandomBot : IBot {
        public int Id { get; private set; }
        public string WorkingDirectory { get; private set; }

        
        private Random random;


        public RandomBot(int id, string workingDirectory = null) {
            random = new Random();
            Id = id;
        }

        public string DoMove(Azul.Board board) {

            var possibleMoves = board.GetValidMoves();
            
            Move option = possibleMoves[random.Next(possibleMoves.Length)];
            
            return $"{option.plateId} {option.tileId} {option.bufferId}";

        }

        public string Place(Azul.Board board) {
            Player me = board.Players[Id];
            var row = me.FullBuffers()[0];
            List<int> possiblePositions = new List<int>();
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if (PossibleCol(me.wall, row, i, me.GetBufferData(row).id)) {
                    possiblePositions.Add(i);
                }
            }

            return $"{possiblePositions[random.Next(possiblePositions.Count)]}";
        }

        public int GetId() => Id;
        
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