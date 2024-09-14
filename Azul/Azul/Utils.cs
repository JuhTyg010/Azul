namespace Azul;

public static class Globals {
    public const int FIRST = 999;
    public const int TYPE_COUNT = 5;
    public const int WALL_DIMENSION = 5;
    public const int TOTAL_TILE_COUNT = 100;
    public const int PLATE_VOLUME = 4;
    public const int EMPTY_CELL = -1;
}

public class IllegalOptionException(string msg) : Exception(msg);

public static class Logger {
    private static string fileName = "azul_log.txt";

    public static void WriteLine(string msg) {
       /* using StreamWriter log = new StreamWriter(Path.Combine(Environment.CurrentDirectory, fileName));
        log.WriteLine(msg);
        log.Flush();*/
        File.AppendAllText(fileName, msg + Environment.NewLine);
    }

    public static void Write(string msg) {
        /*using StreamWriter log = new StreamWriter(Path.Combine(Environment.CurrentDirectory, fileName));
        log.Write(msg);
        log.Flush();*/
        File.AppendAllText(fileName, msg);
    }
}

public enum Phase { Taking = 1, Placing = 2, GameOver = 3 }

public struct Buffer
{
    public int size { get; private set; }
    public int typeId { get; private set; }
    public int filled { get; private set; }
    
    public Buffer(int size) {
        this.size = size;
        typeId = Globals.EMPTY_CELL;
        filled = Globals.EMPTY_CELL;
    }

    public bool Assign(int id, int count) {//TODO:: maybe return int which is leak( what was after limit)
        if (filled != Globals.EMPTY_CELL && typeId != id) {
            Logger.WriteLine($"invalid type, needed {typeId}, got {id}");
            return false; //TODO: break, return some kind of exeption
        }
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
            if(i < filled) outStr += $"{typeId} ";
            else outStr += $"{Globals.EMPTY_CELL} ";
        }
        return outStr;
    }
}

public struct Tile
{
    public int id;
    public int count;

    public Tile(int id, int count)
    {
        this.id = id;
        this.count = count;
    }
}

public struct Vector2
{
    public int x;
    public int y;

    public Vector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}