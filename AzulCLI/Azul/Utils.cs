using System;
using System.IO;

namespace Azul {
    public static class Globals {
        public const int FIRST = 999;
        public const int TYPE_COUNT = 5;
        public const int WALL_DIMENSION = 5;
        public const int TOTAL_TILE_COUNT = 100;
        public const int PLATE_VOLUME = 4;
        public const int EMPTY_CELL = -1;
    }

    public class IllegalOptionException : Exception {
        public IllegalOptionException(string msg) : base(msg) { }
    }

    public static class Logger {
        private static string fileName = "azul_log.txt";

        public static void WriteLine(string msg) {
            File.AppendAllText(fileName, msg + Environment.NewLine);
        }

        public static void Write(string msg) {
            File.AppendAllText(fileName, msg);
        }
    }

    public enum Phase {
        Taking = 1,
        Placing = 2,
        GameOver = 3
    }

    public struct Buffer {
        public int size { get; private set; }
        public int typeId { get; private set; }
        public int filled { get; private set; }

        public Buffer(int size) {
            this.size = size;
            typeId = Globals.EMPTY_CELL;
            filled = Globals.EMPTY_CELL;
        }

        public bool CanAssign(int id) {
            if (filled != Globals.EMPTY_CELL && typeId != id) {
                Logger.WriteLine($"invalid type, needed {typeId}, got {id}");
                return false;
            }
            return true;
        }

        public bool CanAssign(Tile tile) {
            return CanAssign(tile.id);
        }
        
        public bool Assign(int id, int count) {
            if (!CanAssign(id)) return false;

            if (typeId == Globals.EMPTY_CELL) filled = 0;
            typeId = id;
            filled += count;
            filled = Math.Min(filled, size);
            Logger.WriteLine($"successfully filled, current state {filled} of {size}");
            return true;
        }

        public bool Assign(Tile tile) {
            return Assign(tile.id, tile.count);
        }

        public int FreeToFill() {
            if (typeId == Globals.EMPTY_CELL) return size;
            return size - filled;
        }

        public void Clear() {
            typeId = Globals.EMPTY_CELL;
            filled = Globals.EMPTY_CELL;
        }

        public bool IsFull() {
            return size == filled;
        }

        public override string ToString() {
            string outStr = "";
            for (int i = 0; i < size; i++) {
                if (i < filled) outStr += $"{typeId} ";
                else outStr += $"{Globals.EMPTY_CELL} ";
            }

            return outStr;
        }
    }

    public struct Tile {
        public int id;
        public int count;

        public Tile(int id, int count) {
            this.id = id;
            this.count = count;
        }
    }

    public struct Move {
        public int tileId;
        public int plateId;
        public int bufferId;

        public Move(int tileId, int plateId, int bufferId) {
            this.tileId = tileId;
            this.plateId = plateId;
            this.bufferId = bufferId;
        }
    }
}