using System.Diagnostics;

namespace AzulCLI;


public class Tiles
{
    private int[] counts;
    private Random rand = new Random();
    private int totalCount;
    public int typesCount;

    public Tiles(int typesCount, int totalCount) {
        counts = new int[typesCount];
        this.totalCount = totalCount;
        this.typesCount = typesCount;
        int countPerType = typesCount == 0 ? 0 : totalCount / typesCount;
        for (int i = 0; i < typesCount; i++) {
            counts[i] = countPerType;
        }
    }

    private Tiles(int[] counts) {
        this.counts = counts;
        typesCount = counts.Length;
        totalCount = 0;
        foreach (var val in counts) {
            totalCount += val;
        }
    }
    
    public Tiles GetRandom(int count) {
        int[] outCounts = new int[typesCount];
        for (int i = 0; i < count; i++) {
            int chosen = rand.Next(totalCount);
            int id = GetTypeOfTile(chosen);
            outCounts[id]++;
            counts[id]--;
            totalCount--;
        }

        return new Tiles(outCounts);
    }

    public int TileCountOfType(int id) {
        if (id < 0 || id > counts.Length - 1) return 0;
        return counts[id];
    }

    public int TotalTiles() {
        return totalCount;
    }

    public Tile GetTiles(int id) {
        int count = counts[id];
        counts[id] = 0;
        totalCount -= count;
        return new Tile(id, count);
    }

    public void PutTile(int id, int count) {
        counts[id] += count;
        totalCount += count;
    }

    public void PutTile(Tile tile) {
        PutTile(tile.id, tile.count);
    }

    public void Union(Tiles other) {
        // in case one of the counts wasn't initialized yet, if none was it won't do anything 
        if (counts.Length == 0) counts = new int[other.counts.Length];
        if (other.counts.Length == 0) other.counts = new int[counts.Length];
        for (int i = 0; i < counts.Length; i++) {
            counts[i] += other.counts[i];
        }

        totalCount += other.totalCount;
        typesCount = counts.Length;
    }

    private int GetTypeOfTile(int position) {
        Debug.Assert(position < totalCount,"position must be in bounds of all counts");
        int i = 0;
        while (position > counts[i]) {
            position -= counts[i];
            i++;
        }
        return i;
    }
}