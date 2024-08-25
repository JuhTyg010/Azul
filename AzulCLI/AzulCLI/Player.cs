using System.Diagnostics;

namespace AzulCLI;

public class Player
{
    private const int floorSize = 7;
    public string name { get; private set; }
    public int pointCount { get; private set; }
    public int[,] wall { get; private set; }
    private Buffer[] buffers;
    public List<int> floor { get; private set; }
    public bool isFirst { get; private set; }

    public Player(string name) {
        this.name = name;
        pointCount = 0;
        wall = new int[5, 5];
        floor = new List<int>();
        List<Buffer> bufferList = new List<Buffer>();
        for (int i = 0; i < wall.GetLength(0); i++) {
            bufferList.Add(new Buffer(i + 1));
        }

        buffers = bufferList.ToArray();
    }
    
    public bool Place(int row, Tile tile, bool isFirst = false) {
        if (!possibleRow(row, tile.id)) return false;
        if (isFirst) {
            this.isFirst = true;
            floor.Add(Globals.FIRST);
        }
        int toFloor = tile.count - buffers[row].Empty();
        bool answer = buffers[row].Assign(tile);
        
        if (answer && floor.Count < floorSize) {
            //TODO: implement floor addition
        }

        return answer;
    }
    
    public Tile GetBufferData(int row) {
        if (buffers[row].typeId == -1) ; //TODO: return somehow that this is empty
        Debug.Assert(row < buffers.Length, "You're asking for out of range");
        return new Tile(buffers[row].typeId, buffers[row].filled);
    }

    public int[] FullBuffers() {
        if (!hasFullBuffer()) return [];
        List<int> listOfFull = new List<int>();
        for (int i = 0; i < buffers.Length; i++) {
            if(buffers[i].Full()) listOfFull.Add(i);
        }

        return listOfFull.ToArray();
    }

    public bool Fill(int row, int col) {
        if (!buffers[row].Full()) return false;
        //TODO: logs
        if (!possibleColumn(col, buffers[row].typeId)) return false;
        //TODO:logs
        wall[row, col] = buffers[row].typeId;
        //TODO: assign points, check for win condition
        buffers[row].Clear();
        return true;
    }

    public bool ClearFloor() {
        //negative points are -1,-1,-2,-2,-2,-3,-3
        int[] toRemove = { 0, -1, -2, -4, -6, -8, -11, -14 };
        pointCount += toRemove[Math.Min(7, floor.Count)];

        if (floor[0] == Globals.FIRST) {
            floor.Clear();
            return true;
        }
        floor.Clear();
        return false;
    }

    public int FloorSize() {
        return floorSize;
    }
    
    private bool possibleRow(int row, int typeId) {
        for (int i = 0; i < wall.GetLength(1); i++) {
            if (wall[row, i] == typeId) return false;
        }

        return true;
    }

    private bool possibleColumn(int col, int typeId) {
        for (int i = 0; i < wall.GetLength(0); i++) {
            if (wall[i, col] == typeId) return false;
        }

        return true;
    }

    public bool hasFullBuffer() {
        foreach (var buffer in buffers) {
            if (buffer.Full()) return true;
        }
        return false;
    }
}