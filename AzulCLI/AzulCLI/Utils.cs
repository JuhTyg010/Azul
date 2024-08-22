namespace AzulCLI;

public struct Buffer
{
    public int size { get; private set; }
    public int typeId { get; private set; }
    public int filled { get; private set; }
    
    public Buffer(int size) {
        this.size = size;
        typeId = -1;
        filled = -1;
    }

    public bool Assign(int id, int count) {//TODO:: maybe return int which is leak( what was after limit)
        if (filled != -1) return false; //TODO: break, return some kind of exeption
        typeId = id;
        filled += Math.Min(count, size);
        return true;
    }

    public bool Assign(Tile tile) {
        return Assign(tile.id, tile.count);
    }

    public int Empty() {
        if (typeId == -1) return size;
        return size - filled;
    }

    public void Clear() {
        typeId = -1;
        filled = -1;
    }

    public bool Full() {
        return size == filled;
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