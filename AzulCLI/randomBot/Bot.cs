using System;
using System.Collections.Generic;
using Azul;
using Random = System.Random;

namespace randomBot {

    internal struct Option {
        public int plate;
        public int tile;
        public int buffer;
        
        public Option(int plate, int tile, int buffer) {
            this.plate = plate;
            this.tile = tile;
            this.buffer = buffer;
        }
    }
    public class Bot : IBot {
        private Random random;
        public int id { get; private set; }

        public Bot(int _id) {
            random = new Random();
            id = _id;
        }

        public string DoMove(Azul.Board board) {
            
            var possibleMoves = PossibleMoves(board);
            int index = random.Next(possibleMoves.Count);
            Option option = new Option();
            int bestGain = Int32.MinValue;
            foreach (var possibleMove in possibleMoves) {
                int gain = GainIfPlayed(possibleMove, board);
                if (gain > bestGain) {
                    option = possibleMove;
                    bestGain = gain;
                }
            }
            return $"{option.plate} {option.tile} {option.buffer}";

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

        private List<Option> PossibleMoves(Azul.Board board) {
            int plateCount = board.Plates.Length;
            List<Option> possibleMoves = new List<Option>();
            for (int plate = 0; plate <= plateCount; plate++) {
                for (int type = 0; type < Globals.TYPE_COUNT; type++) {
                    for (int buffer = 0; buffer <= Globals.TYPE_COUNT; buffer++) {  //equal for floor
                        if (board.CanMove(plate, type, buffer)) {
                            possibleMoves.Add(new Option(plate, type, buffer));
                        }
                    }
                }
            }
            return possibleMoves;
        }

        
        private int GainIfPlayed(Option possibleMove, Azul.Board board) {
            int gain = 0;
            if (possibleMove.buffer >= Globals.WALL_DIMENSION) {
                return -10;
            }
            Player me = board.Players[id];
            int bufferSize = possibleMove.buffer + 1;
            Tile buffTile = me.GetBufferData(possibleMove.buffer);
            Plate p = possibleMove.plate < board.Plates.Length ? board.Plates[possibleMove.plate] : board.Center;
            int toFill = p.TileCountOfType(possibleMove.tile);
            if (buffTile.id == possibleMove.tile) {
                int toFloor = toFill - (bufferSize - buffTile.count);
                if (toFloor >= 0) {
                    gain -= toFloor;
                    int clearGain = 0;
                    if (board.isAdvanced) {
                        int currGain = 0;
                        for (int col = 0; col < Globals.WALL_DIMENSION; col++) {
                            currGain = me.CalculatePointsIfFilled(possibleMove.buffer, col);
                            if(currGain > clearGain) clearGain = currGain;
                        }
                    }
                    else {
                        int row = possibleMove.buffer;
                        int col = 0;
                        for(;col < Globals.WALL_DIMENSION; col++)
                            if (board.predefinedWall[row, col] == possibleMove.tile)
                                break;
                        clearGain = me.CalculatePointsIfFilled(row,col);
                    }
                    gain += clearGain;
                }
            }
            else {
                int toFloor = bufferSize - toFill;
                if (toFloor >= 0) {
                    gain -= toFloor;
                    int clearGain = 0;
                    if (board.isAdvanced) {
                        int currGain = 0;
                        for (int col = 0; col < Globals.WALL_DIMENSION; col++) {
                            currGain = me.CalculatePointsIfFilled(possibleMove.buffer, col);
                            if(currGain > clearGain) clearGain = currGain;
                        }
                    }
                    else {
                        int row = possibleMove.buffer;
                        int col = 0;
                        for(;col < Globals.WALL_DIMENSION; col++)
                            if (board.predefinedWall[row, col] == possibleMove.tile)
                                break;
                        clearGain = me.CalculatePointsIfFilled(row,col);
                    }
                    gain += clearGain;
                }
            }
            
            return gain;
        }
    }
}