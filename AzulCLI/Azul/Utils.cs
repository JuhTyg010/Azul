using System;
using System.IO;

namespace Azul {
    public static class Globals {
        public const int First = 999;
        public const int TypeCount = 5;
        public const int WallDimension = 5;
        public const int TotalTileCount = 100;
        public const int PlateVolume = 4;
        public const int EmptyCell = -1;
        public const int FloorSize = 7;
        public const int BufferCount = WallDimension;
        public static readonly Move DefaultMove = new Move(-1,-1,-1);
    }

    public class IllegalOptionException : Exception {
        public IllegalOptionException(string msg) : base(msg) { }
    }

    public static class Logger {
        private static string _fileName = "log.txt";

        public static void SetName(string name) {
            _fileName = name;
        }
        public static void WriteLine(string msg) {
            File.AppendAllText(_fileName, msg + Environment.NewLine);
        }

        public static void Write(string msg) {
            File.AppendAllText(_fileName, msg);
        }
    }

    /// <summary>
    /// Enum to recognise current state
    /// </summary>
    public enum Phase {
        Taking = 1,
        Placing = 2,
        GameOver = 3
    }

    /// <summary>
    /// struct to get buffer data
    /// </summary>
    public struct Buffer {
        public int Size { get; private set; }
        public int TypeId { get; private set; }
        public int CurrentlyFilled { get; private set; }

        public Buffer(int size) {
            this.Size = size;
            TypeId = Globals.EmptyCell;
            CurrentlyFilled = Globals.EmptyCell;
        }

        public bool CanAssign(int id) {
            if (CurrentlyFilled != Globals.EmptyCell && TypeId != id) {
                Logger.WriteLine($"invalid type, needed {TypeId}, got {id}");
                return false;
            }
            return true;
        }

        public bool CanAssign(Tile tile) {
            return CanAssign(tile.Id);
        }
        
        internal bool Assign(int id, int count) {
            if (!CanAssign(id)) return false;

            if (TypeId == Globals.EmptyCell) CurrentlyFilled = 0;
            TypeId = id;
            CurrentlyFilled += count;
            CurrentlyFilled = Math.Min(CurrentlyFilled, Size);
            Logger.WriteLine($"successfully filled, current state {CurrentlyFilled} of {Size}");
            return true;
        }

        internal bool Assign(Tile tile) {
            return Assign(tile.Id, tile.Count);
        }

        public int FreeToFill() {
            if (TypeId == Globals.EmptyCell) return Size;
            return Size - CurrentlyFilled;
        }

        internal void Clear() {
            TypeId = Globals.EmptyCell;
            CurrentlyFilled = Globals.EmptyCell;
        }

        public bool IsFull() {
            return Size == CurrentlyFilled;
        }

        public override string ToString() {
            string outStr = "";
            for (int i = 0; i < Size; i++) {
                if (i < CurrentlyFilled) outStr += $"{TypeId} ";
                else outStr += $"{Globals.EmptyCell} ";
            }

            return outStr;
        }
    }

    public struct Tile {
        public int Id;
        public int Count;

        public Tile(int id, int count) {
            this.Id = id;
            this.Count = count;
        }
    }

    public record Move {
        public readonly int TileId;
        public readonly int PlateId;
        public readonly int BufferId;

        public Move(int tileId, int plateId, int bufferId) {
            this.TileId = tileId;
            this.PlateId = plateId;
            this.BufferId = bufferId;
        }

        public override string ToString() {
            return $"{PlateId} {TileId} {BufferId}";
        }
    }
    
}