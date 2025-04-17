using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Azul {
    public class Player
    {
        public string name { get; private set; }
        public int pointCount { get; private set; }
        public int[,] wall { get; private set; }
        public List<int> floor { get; private set; }
        public bool isFirst { get; private set; }
        
        private Buffer[] buffers;
        
        public event EventHandler? OnWin;

        /// <summary>
        /// Function to calculate how much points are given when filled to wall
        /// </summary>
        /// <param name="row">row of filling</param>
        /// <param name="col">column of filling</param>
        /// <param name="wall"> wall for which we are counting </param>
        /// <returns> number of point awarded when filled </returns>
        public static int CalculatePointReward(int row, int col, int[,] wall) {
            int colPoints = 0;
            int rowPoints = 0;
            int colTmp = col;
            while (colTmp >= 0 && wall[row, colTmp] != Globals.EmptyCell || colTmp == col) {
                colPoints++;
                colTmp--;
            }

            colTmp = col + 1;
            while (colTmp < wall.GetLength(1) &&
                   wall[row, colTmp] != Globals.EmptyCell) {
                
                colPoints++;
                colTmp++;
            }
            
            int rowTmp = row;
            while (rowTmp >= 0 && wall[rowTmp, col] != Globals.EmptyCell || row == rowTmp) {
                rowPoints++;
                rowTmp--;
            }
            rowTmp = row + 1;
            while (rowTmp < wall.GetLength(0) &&
                   wall[rowTmp, col] != Globals.EmptyCell) {
                rowPoints++;
                rowTmp++;
            }

            if (rowPoints > 1 && colPoints > 1) return rowPoints + colPoints;
        
            return Math.Max(colPoints, rowPoints); // at least one is 1
        }

        internal Player(Buffer[] buffers, List<int> floor, bool isFirst, int[,] wall) {
            this.buffers = buffers;
            this.floor = floor;
            this.isFirst = isFirst;
            this.wall = wall;
        }
        internal Player(string name) {
            this.name = name;
            pointCount = 0;
            wall = new int[Globals.WallDimension, Globals.WallDimension];
            floor = new List<int>();
            List<Buffer> bufferList = new List<Buffer>();
            for (int i = 0; i < wall.GetLength(0); i++) {
                bufferList.Add(new Buffer(i + 1));
                for (int j = 0; j < wall.GetLength(1); j++) {
                    wall[i, j] = Globals.EmptyCell;
                }
            }

            buffers = bufferList.ToArray();
        }

        public bool CanPlace(int row, int tileId) {
            if (row == Globals.WallDimension) return true;
            if (!possibleRow(row, tileId)) return false;
            if (!possibleBuffer(row, tileId)) return false;
            
            return buffers[row].CanAssign(tileId);
        }
        
        public bool CanPlace(int row, Tile tile) {
            return CanPlace(row, tile.Id);
        }
        
        
        internal bool Place(int row, Tile tile, bool isFirst = false) {   //row can be Globals.WALL_DIMENSION for floor
            
            bool canPlace = CanPlace(row, tile);
            if (canPlace) {
                if (row == Globals.WallDimension) {
                    Logger.WriteLine("is on floor");
                    for (int i = 0; i < tile.Count; i++) {
                        if (floor.Count < Globals.FloorSize) floor.Add(tile.Id);
                        else break;
                    }
                    return true;
                }
                
                if (isFirst) {
                    this.isFirst = true;
                    floor.Add(Globals.First);
                }
                int toFloor = tile.Count - buffers[row].FreeToFill();
                buffers[row].Assign(tile);
        
                if (floor.Count < Globals.FloorSize) {
                    for (int i = 0; i < toFloor; i++) {
                        floor.Add(tile.Id);
                        if (floor.Count == Globals.FloorSize) {
                            break;
                        }
                    }
                }
            }
            else {
                if (!possibleRow(row, tile.Id)) {
                    Logger.WriteLine("invalid row");
                    return false;
                }
                if (!possibleBuffer(row, tile.Id)) {
                    Logger.WriteLine("invalid buffer");
                    return false;
                }
            }
            return canPlace;
        }
    
        public Tile GetBufferData(int row) {
            Debug.Assert(row < buffers.Length && row >= 0, 
                "You're asking for out of range");
            return new Tile(buffers[row].TypeId, buffers[row].CurrentlyFilled);
        }

        public int[] GetFullBuffersIds() {
            if (!HasFullBuffer()) return new int[]{};
            List<int> listOfFull = new List<int>();
            for (int i = 0; i < buffers.Length; i++) {
                if(buffers[i].IsFull()) listOfFull.Add(i);
            }

            return listOfFull.ToArray();
        }

        internal bool Fill(int row, int col, Board board) {
            if (!buffers[row].IsFull()) {
                Logger.WriteLine("buffer is not full");
                return false;
            }

            if (!possibleColumn(col, buffers[row].TypeId)) {
                Logger.WriteLine("invalid column to fill");
                return false;
            }
            wall[row, col] = buffers[row].TypeId;
            pointCount += AddedPointsAfterFilled(row, col);
            board.PutToTrash(buffers[row].TypeId, buffers[row].Size - 1);    //one goes on wall
            
            Logger.WriteLine(
                $"successfully placed on wall new points: {pointCount}");

            if (IsWinCheck()) {
                Logger.WriteLine($"{this} win");
                OnWin?.Invoke(this, EventArgs.Empty);
            }
        
            buffers[row].Clear();
            return true;
        }

        public static bool Fill(int row, int col, ref Player p) {
            if (!p.buffers[row].IsFull()) return false;
            if (!p.possibleColumn(col, p.buffers[row].TypeId)) return false;
            
            p.wall[row, col] = p.buffers[row].TypeId;
            p.pointCount += p.AddedPointsAfterFilled(row, col);
            p.buffers[row].Clear();
            return true;
        }

        internal bool ClearFloor(Board board) {
            //negative points are -1,-1,-2,-2,-2,-3,-3
            int[] toRemove = { 0, -1, -2, -4, -6, -8, -11, -14 };
            bool isFirst = false;
            pointCount += toRemove[Math.Min(7, floor.Count)];
            Logger.WriteLine(
                $"Player {name} is clearing the floor new points {pointCount}");

            for (int i = 0; i < floor.Count; i++) {
                if (floor[i] == Globals.First) {
                    isFirst = true;
                }
                else {
                    board.PutToTrash(floor[i],1);
                }
            }
            floor.Clear();

            return isFirst;
        }
    
        public bool HasFullBuffer() {
            foreach (var buffer in buffers) {
                if (buffer.IsFull()) return true;
            }
            return false;
        }

        public bool IsEqual(Player player) {
            if(player.name != name) return false;
            if(player.pointCount != pointCount) return false;
            if(player.wall != wall) return false;
            return true;
        }
        internal void CalculateBonusPoints() {
            int fullColumns = 0;
            int fullRows = 0;
            int fullTypes = 0;
            int[] totalCounts = new int[Globals.TypeCount];

            for (int i = 0; i < Globals.WallDimension; i++) {
                bool isEmptyInColumn = false;
                bool isEmptyInRow = false;
                for (int j = 0; j < Globals.WallDimension; j++) {
                    if (wall[i, j] == Globals.EmptyCell) isEmptyInRow = true;
                    if (wall[j, i] == Globals.EmptyCell) isEmptyInColumn = true;
                    if (wall[i, j] != Globals.EmptyCell) totalCounts[wall[i, j]]++;
                }

                if (!isEmptyInColumn) fullColumns++;
                if (!isEmptyInRow) fullRows++;
            }

            foreach (var type in totalCounts) {
                if (type == Globals.WallDimension) fullTypes++;
            }
            
            pointCount += fullColumns * 7;
            pointCount += fullRows * 2;
            pointCount += fullTypes * 10;

        }

        public override string ToString() {
            
            string player = $"{name} points: {pointCount}\n";
            string board = "";
            for (int i = 0; i < Globals.WallDimension; i++) {
                board += new String(' ', (Globals.WallDimension - i)*2);
                board += $" {buffers[i]} -> ";
                for (int j = 0; j < wall.GetLength(1); j++) {
                    board += $"{wall[i, j].ToString()} ";
                }
                board += "\n";
            }
            player += board.Replace($"{Globals.EmptyCell}", "_");
            string floorStr = "floor: ";
            for (int i = 0; i < floor.Count; i++) {
                floorStr += $" {floor[i].ToString()}";
            }
            floorStr += "\n";
            player += floorStr.Replace($"{Globals.First}", "F");
            return player;
        }

        private bool possibleRow(int row, int typeId) {
            for (int i = 0; i < wall.GetLength(1); i++) {
                if (wall[row, i] == typeId) return false;
            }

            return true;
        }

        private bool possibleBuffer(int bufferId, int typeId) {
            if (buffers[bufferId].TypeId == Globals.EmptyCell ||
                buffers[bufferId].TypeId == typeId) return true;
            return false;
        }

        private bool possibleColumn(int col, int typeId) {
            for (int i = 0; i < wall.GetLength(0); i++) {
                if (wall[i, col] == typeId) return false;
            }

            return true;
        }

        /// <summary>
        /// Function to calculate how much points are given when filled to wall
        /// </summary>
        /// <param name="row">row of filling</param>
        /// <param name="col">column of filling</param>
        /// <returns> number of point awarded when filled </returns>
        public int AddedPointsAfterFilled(int row, int col) {
            return Player.CalculatePointReward(row, col, wall);
        }

        private bool IsWinCheck() {
            for (int row = 0; row < wall.GetLength(0); row++) {
                var isSpace = false;
                for (int col = 0; col < wall.GetLength(1); col++) {
                    if (wall[row, col] == Globals.EmptyCell) {
                        isSpace = true;
                        break;
                    }
                }
                if (!isSpace) return true;
            }
            return false;
        }
    }
}