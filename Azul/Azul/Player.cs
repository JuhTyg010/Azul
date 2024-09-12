using System.Diagnostics;

namespace Azul;

public class Player
{
    private const int floorSize = 7;
    public string name { get; private set; }
    public int pointCount { get; private set; }
    public int[,] wall { get; private set; }
    private Buffer[] buffers;
    public List<int> floor { get; private set; }
    public bool isFirst { get; private set; }

    public event EventHandler? OnWin;
    

    public Player(string name) {
        this.name = name;
        pointCount = 0;
        wall = new int[5, 5];
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
    
    public bool Place(int row, Tile tile, bool isFirst = false) {   //row can be Globals.WALL_DIMENSION for floor
        if (row == Globals.WALL_DIMENSION) {
            for (int i = 0; i < tile.count; i++) {
                if (floor.Count < floorSize) floor.Add(tile.id);
                else break;
            }

            return true;
        }
        if (!possibleRow(row, tile.id)) return false;
        if (!possibleBuffer(row, tile.id)) return false;
        if (isFirst) {
            this.isFirst = true;
            floor.Add(Globals.FIRST);
        }
        int toFloor = tile.count - buffers[row].FreeToFill();
        bool answer = buffers[row].Assign(tile);
        
        if (answer && floor.Count < floorSize) {
            for (int i = 0; i < toFloor; i++) {
                floor.Add(tile.id);
                if (floor.Count == floorSize) {
                    break;
                }
            }
        }

        return answer;
    }
    
    public Tile GetBufferData(int row) {
        Debug.Assert(row < buffers.Length, "You're asking for out of range");
        return new Tile(buffers[row].typeId, buffers[row].filled);
    }

    public int[] FullBuffers() {
        if (!hasFullBuffer()) return [];
        List<int> listOfFull = new List<int>();
        for (int i = 0; i < buffers.Length; i++) {
            if(buffers[i].IsFull()) listOfFull.Add(i);
        }

        return listOfFull.ToArray();
    }

    public bool Fill(int row, int col) {
        if (!buffers[row].IsFull()) return false;
        //TODO: logs
        if (!possibleColumn(col, buffers[row].typeId)) return false;
        //TODO:logs
        wall[row, col] = buffers[row].typeId;
        pointCount += calculatePoints(row, col);

        if (IsWinCheck()) {
            OnWin?.Invoke(this, EventArgs.Empty);
        }
        //TODO: start event or something
        
        buffers[row].Clear();
        return true;
    }

    public bool ClearFloor() {
        //negative points are -1,-1,-2,-2,-2,-3,-3
        int[] toRemove = { 0, -1, -2, -4, -6, -8, -11, -14 };
        pointCount += toRemove[Math.Min(7, floor.Count)];

        if (floor.Count != 0 && floor[0] == Globals.FIRST) {
            floor.Clear();
            return true;
        }
        floor.Clear();
        return false;
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

    private int calculatePoints(int row, int col) {
        int colPoints = 0;
        int rowPoints = 0;
        int colTmp = col;
        int rowTmp = row;
        while (colTmp >= 0 && wall[row, colTmp] != Globals.EMPTY_CELL) {
            colPoints++;
            colTmp--;
        }

        colTmp = col + 1;
        while (colTmp < wall.GetLength(1) && wall[row, colTmp] != Globals.EMPTY_CELL) {
            colPoints++;
            colTmp++;
        }
        
        while (rowTmp >= 0 && wall[row, rowTmp] != Globals.EMPTY_CELL) {
            rowPoints++;
            rowTmp--;
        }
        rowTmp = row + 1;
        while (rowTmp < wall.GetLength(0) && wall[row, rowTmp] != Globals.EMPTY_CELL) {
            rowPoints++;
            rowTmp++;
        }

        if (rowTmp > 1 && colTmp > 1) return rowPoints + colPoints;
        
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