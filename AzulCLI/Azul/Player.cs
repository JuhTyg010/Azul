using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Azul {
    public class Player
    {
        private const int floorSize = 7;
        public string name { get; private set; }
        public int pointCount { get; private set; }
        public int[,] wall { get; private set; }
        public List<int> floor { get; private set; }
        public bool isFirst { get; private set; }
        
        private Buffer[] buffers;
        private Board game;
        
        public event EventHandler? OnWin;
    

        public Player(string name, Board game) {
            this.name = name;
            this.game = game;
            pointCount = 0;
            wall = new int[Globals.WALL_DIMENSION, Globals.WALL_DIMENSION];
            floor = new List<int>();
            List<Buffer> bufferList = new List<Buffer>();
            for (int i = 0; i < wall.GetLength(0); i++) {
                bufferList.Add(new Buffer(i + 1));
                for (int j = 0; j < wall.GetLength(1); j++) {
                    wall[i, j] = Globals.EMPTY_CELL;
                }
            }

            buffers = bufferList.ToArray();
        }

        public bool CanPlace(int row, int tileId) {
            if (row == Globals.WALL_DIMENSION) return true;
            if (!possibleRow(row, tileId)) return false;
            if (!possibleBuffer(row, tileId)) return false;
            
            return buffers[row].CanAssign(tileId);
        }
        
        public bool CanPlace(int row, Tile tile) {
            return CanPlace(row, tile.id);
        }
        
        public bool Place(int row, Tile tile, bool isFirst = false) {   //row can be Globals.WALL_DIMENSION for floor
            
            bool canPlace = CanPlace(row, tile);
            if (canPlace) {
                if (row == Globals.WALL_DIMENSION) {
                    Logger.WriteLine("is on floor");
                    for (int i = 0; i < tile.count; i++) {
                        if (floor.Count < floorSize) floor.Add(tile.id);
                        else break;
                    }
                    return true;
                }
                
                if (isFirst) {
                    this.isFirst = true;
                    floor.Add(Globals.FIRST);
                }
                int toFloor = tile.count - buffers[row].FreeToFill();
                buffers[row].Assign(tile);
        
                if (floor.Count < floorSize) {
                    for (int i = 0; i < toFloor; i++) {
                        floor.Add(tile.id);
                        if (floor.Count == floorSize) {
                            break;
                        }
                    }
                }
            }
            else {
                if (!possibleRow(row, tile.id)) {
                    Logger.WriteLine("invalid row");
                    return false;
                }
                if (!possibleBuffer(row, tile.id)) {
                    Logger.WriteLine("invalid buffer");
                    return false;
                }
            }
            return canPlace;
        }
    
        public Tile GetBufferData(int row) {
            Debug.Assert(row < buffers.Length, 
                "You're asking for out of range");
            return new Tile(buffers[row].typeId, buffers[row].filled);
        }

        public int[] FullBuffers() {
            if (!hasFullBuffer()) return new int[]{};
            List<int> listOfFull = new List<int>();
            for (int i = 0; i < buffers.Length; i++) {
                if(buffers[i].IsFull()) listOfFull.Add(i);
            }

            return listOfFull.ToArray();
        }

        public bool Fill(int row, int col) {
            if (!buffers[row].IsFull()) {
                Logger.WriteLine("buffer is not full");
                return false;
            }

            if (!possibleColumn(col, buffers[row].typeId)) {
                Logger.WriteLine("invalid column to fill");
                return false;
            }
            wall[row, col] = buffers[row].typeId;
            pointCount += calculatePoints(row, col);
            game.PutToTrash(buffers[row].typeId, buffers[row].size - 1);    //one goes on wall
            
            Logger.WriteLine(
                $"successfully placed on wall new points: {pointCount}");

            if (IsWinCheck()) {
                Logger.WriteLine($"{this} win");
                OnWin?.Invoke(this, EventArgs.Empty);
            }
        
            buffers[row].Clear();
            return true;
        }

        public bool ClearFloor() {
            //negative points are -1,-1,-2,-2,-2,-3,-3
            int[] toRemove = { 0, -1, -2, -4, -6, -8, -11, -14 };
            bool isFirst = false;
            pointCount += toRemove[Math.Min(7, floor.Count)];
            Logger.WriteLine(
                $"Player {name} is clearing the floor new points {pointCount}");

            for (int i = 0; i < floor.Count; i++) {
                if (floor[i] == Globals.FIRST) {
                    isFirst = true;
                }
                else {
                    game.PutToTrash(floor[i],1);
                }
            }
            floor.Clear();

            return isFirst;
        }

        public int FloorSize() {
            return floorSize;
        }
    
        public bool hasFullBuffer() {
            foreach (var buffer in buffers) {
                if (buffer.IsFull()) return true;
            }
            return false;
        }

        public void CalculateBonusPoints() {
            int fullColumns = 0;
            int fullRows = 0;
            int fullTypes = 0;
            int[] totalCounts = new int[Globals.TYPE_COUNT];

            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                bool isEmptyInColumn = false;
                bool isEmptyInRow = false;
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    if (wall[i, j] == Globals.EMPTY_CELL) isEmptyInRow = true;
                    if (wall[j, i] == Globals.EMPTY_CELL) isEmptyInColumn = true;
                    if (wall[i, j] != Globals.EMPTY_CELL) totalCounts[wall[i, j]]++;
                }

                if (!isEmptyInColumn) fullColumns++;
                if (!isEmptyInRow) fullRows++;
            }

            foreach (var type in totalCounts) {
                if (type == Globals.WALL_DIMENSION) fullTypes++;
            }
            
            pointCount += fullColumns * 7;
            pointCount += fullRows * 2;
            pointCount += fullTypes * 10;

        }

        public override string ToString() {
            string board = "";
            for (int i = 0; i < wall.GetLength(0); i++) {
                board += " [";
                board += buffers[i].ToString();
                board += "] [";
                for (int j = 0; j < wall.GetLength(1); j++) {
                    board += wall[i, j].ToString();
                }
                board += "]";
            }

            return $"{name} ({pointCount}) {board} {floor}";
        }

        private bool possibleRow(int row, int typeId) {
            for (int i = 0; i < wall.GetLength(1); i++) {
                if (wall[row, i] == typeId) return false;
            }

            return true;
        }

        private bool possibleBuffer(int bufferId, int typeId) {
            if (buffers[bufferId].typeId == Globals.EMPTY_CELL ||
                buffers[bufferId].typeId == typeId) return true;
            return false;
        }

        private bool possibleColumn(int col, int typeId) {
            for (int i = 0; i < wall.GetLength(0); i++) {
                if (wall[i, col] == typeId) return false;
            }

            return true;
        }

        public int CalculatePointsIfFilled(int row, int col) {
            int colPoints = 0;
            int rowPoints = 0;
            int colTmp = col;
            while (colTmp >= 0 && wall[row, colTmp] != Globals.EMPTY_CELL || colTmp == col) {
                colPoints++;
                colTmp--;
            }

            colTmp = col + 1;
            while (colTmp < wall.GetLength(1) &&
                   wall[row, colTmp] != Globals.EMPTY_CELL) {
                
                colPoints++;
                colTmp++;
            }
            
            int rowTmp = row;
            while (rowTmp >= 0 && wall[rowTmp, col] != Globals.EMPTY_CELL || row == rowTmp) {
                rowPoints++;
                rowTmp--;
            }
            rowTmp = row + 1;
            while (rowTmp < wall.GetLength(0) &&
                   wall[rowTmp, col] != Globals.EMPTY_CELL) {
                rowPoints++;
                rowTmp++;
            }

            if (rowPoints > 1 && colPoints > 1) return rowPoints + colPoints;
            
        
            return Math.Max(colPoints, rowPoints); // at least one is 1
        }
        private int calculatePoints(int row, int col) {
            int colPoints = 0;
            int rowPoints = 0;
            int colTmp = col;
            while (colTmp >= 0 && wall[row, colTmp] != Globals.EMPTY_CELL) {
                colPoints++;
                colTmp--;
            }

            colTmp = col + 1;
            while (colTmp < wall.GetLength(1) &&
                   wall[row, colTmp] != Globals.EMPTY_CELL) {
                
                colPoints++;
                colTmp++;
            }
            
            int rowTmp = row;
            while (rowTmp >= 0 && wall[rowTmp, col] != Globals.EMPTY_CELL) {
                rowPoints++;
                rowTmp--;
            }
            rowTmp = row + 1;
            while (rowTmp < wall.GetLength(0) &&
                   wall[rowTmp, col] != Globals.EMPTY_CELL) {
                rowPoints++;
                rowTmp++;
            }

            if (rowPoints > 1 && colPoints > 1) return rowPoints + colPoints;
            
        
            return Math.Max(colPoints, rowPoints); // at least one is 1
        }

        private bool IsWinCheck() {
            for (int row = 0; row < wall.GetLength(0); row++) {
                var isSpace = false;
                for (int col = 0; col < wall.GetLength(1); col++) {
                    if (wall[row, col] == Globals.EMPTY_CELL) {
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