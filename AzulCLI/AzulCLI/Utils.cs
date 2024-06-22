namespace AzulCLI;

public struct Buffer
{
    public int size { get; private set; }
    public int typeId { get; private set; }
    public int filled { get; private set; }
    
    public Buffer(int size)
    {
        this.size = size;
        typeId = 0;
        filled = 0;
    }

    public bool Asign(int id, int count)//TODO:: maybe return int which is leak( what was after limit)
    {
        if (filled != 0) return false; //TODO: break, return some kind of exeption
        typeId = id;
        filled += count;
        return true;
    }

    public void Clear()
    {
        typeId = 0;
        filled = 0;
    }

    public bool Full()
    {
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