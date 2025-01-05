using System.Collections.Generic;
using Azul;
using UnityEngine;
using Random = System.Random;

namespace randomBot {

    public class Bot {
        private Random random;
        public int id { get; private set; }

        public Bot(int _id) {
            random = new Random();
            id = _id;
        }

        public string DoMove(Azul.Board board) {
            /*Player me = board.Players[id];
            List<int> possibleBuffers = PossibleBuffers(me);
            List<int> possibleTypes = new List<int>();

            bool isCorrectConfig = false;
            int chosenBuffer = Globals.EMPTY_CELL;
            int chosenType = Globals.EMPTY_CELL; //not chosen yet
            int chosenPlate = Globals.EMPTY_CELL; //not chosen yet
            int randomPos;

            while (!isCorrectConfig) {
                if (possibleBuffers.Count == 0) {
                    chosenBuffer = Globals.WALL_DIMENSION; //floor
                    possibleTypes = new List<int>();
                    for (int i = 0; i < Globals.TYPE_COUNT; i++) possibleTypes.Add(i); //every type is possible 
                }
                else {
                    randomPos = random.Next(0, possibleBuffers.Count);
                    chosenBuffer = possibleBuffers[randomPos];
                    possibleBuffers.RemoveAt(randomPos);
                    possibleTypes = PossibleTypes(me, chosenBuffer);
                }

                while (true) {
                    if (possibleTypes.Count == 0) break;
                    randomPos = random.Next(0, possibleTypes.Count);
                    chosenType = possibleTypes[randomPos];
                    possibleTypes.RemoveAt(randomPos);

                    List<int> possiblePlates = PossiblePlates(board, chosenType);
                    if (possiblePlates.Count > 0) {
                        isCorrectConfig = true;
                        randomPos = random.Next(0, possiblePlates.Count);
                        chosenPlate = possiblePlates[randomPos];
                        break;
                    }
                }
            }

            return $"{chosenPlate} {chosenType} {chosenBuffer}";*/
            var possibleMoves = PossibleMoves(board);
            int index = random.Next(possibleMoves.Count);
            Debug.Log($"Possible moves: {possibleMoves.Count}");
            //TODO: select one with higher point gain, aka prefer to not put tiles to the floor
            int[] move = possibleMoves[index];
            return $"{move[0]} {move[1]} {move[2]}";

        }

        public string Place(Azul.Board board) {
            Player me = board.Players[id];
            var row = me.FullBuffers()[0];
            List<int> possiblePositions = new List<int>();
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if (PossibleCol(me.wall, row, i, me.GetBufferData(row).id)) {
                    possiblePositions.Add(i);
                }
            }

            return $"{possiblePositions[random.Next(possiblePositions.Count)]}";
        }

        private bool PossibleCol(int[,] wall, int row, int column, int chosenType) {
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if (wall[i, column] == chosenType) {
                    return false;
                }
            }

            return wall[row, column] == Globals.EMPTY_CELL;
        }

        private List<int> PossiblePlates(Azul.Board board, int chosenType) {
            List<int> possiblePlates = new List<int>();

            for (int i = 0; i < board.Plates.Length; i++) {
                if (board.Plates[i].GetCounts()[chosenType].count > 0) {
                    possiblePlates.Add(i);
                }
            }

            if (board.Center.GetCounts()[chosenType].count > 0) possiblePlates.Add(board.Plates.Length);
            return possiblePlates;
        }

        private List<int> PossibleTypes(Player me, int chosenBuffer) {
            List<int> possibleTypes = new List<int>();
            if (me.GetBufferData(chosenBuffer).id != Globals.EMPTY_CELL)
                return new List<int>() { me.GetBufferData(chosenBuffer).id };
            for (int i = 0; i < Globals.TYPE_COUNT; i++) {
                bool isPossible = true;
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    if (me.wall[chosenBuffer, j] == i) {
                        isPossible = false;
                        break;
                    }
                }

                if (isPossible) {
                    for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                        if (PossibleCol(me.wall, chosenBuffer, j, i)) {
                            possibleTypes.Add(i);
                            break;
                        }
                    }
                }
            }

            return possibleTypes;
        }

        private List<int> PossibleBuffers(Player me) {
            List<int> posibleBuffers = new List<int>();
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                var data = me.GetBufferData(i);
                if (data.count < i + 1) {
                    posibleBuffers.Add(i);
                }
            }

            return posibleBuffers;
        }

        private List<int[]> PossibleMoves(Azul.Board board) {
            int plateCount = board.Plates.Length;
            List<int[]> possibleMoves = new List<int[]>();
            for (int plate = 0; plate <= plateCount; plate++) {
                for (int buffer = 0; buffer < Globals.TYPE_COUNT; buffer++) {
                    for (int type = 0; type < Globals.TYPE_COUNT; type++) {
                        if (board.CanMove(plate, type, buffer)) {
                            possibleMoves.Add(new int[] {plate, type, buffer});
                        }
                    }
                }
            }
            return possibleMoves;
        }
    }
}