using System.Diagnostics;

namespace AzulCLI;
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

public struct Tiles
{
    private int[] counts;
    private Random rand = new Random();
    private int totalCount;

    public Tiles(int typesCount, int totalCount)
    {
        counts = new int[typesCount];
        this.totalCount = totalCount;
        int countPerType = totalCount / typesCount;
        for (int i = 0; i < typesCount; i++)
        {
            counts[i] = countPerType;
        }
    }

    private Tiles(int[] counts)
    {
        this.counts = counts;
    }

    public Tiles GetRandom(int count)
    {
        int[] outCounts = new int[counts.Length];
        for (int i = 0; i < count; i++)
        {
            int chosen = rand.Next(totalCount);
            int id = GetTypeOfTile(chosen);
            outCounts[id]++;
            counts[id]--;
            totalCount--;
        }

        return new Tiles(outCounts);
    }

    public int CountOfTiles(int id)
    {
        return counts[id];
    }

    public int TotalTiles()
    {
        return totalCount;
    }

    public Tile GetTiles(int id)
    {
        int count = counts[id];
        counts[id] = 0;
        return new Tile(id, count);
    }

    public void PutTile(int id, int count)
    {
        counts[id] += count;
    }

    public void PutTile(Tile tile)
    {
        PutTile(tile.id, tile.count);
    }

    public void Union(Tiles other)
    {
        for (int i = 0; i < counts.Length; i++)
        {
            counts[i] += other.counts[i];
        }
    }

    private int GetTypeOfTile(int position)
    {
        Debug.Assert(position >= totalCount);
        int i = 0;
        while (position > counts[i])
        {
            position -= counts[i];
            i++;
        }
        return i;
    }
}