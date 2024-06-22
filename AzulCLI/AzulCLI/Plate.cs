using System.Diagnostics;

namespace AzulCLI;

public class Plate
{
    protected Tiles tiles;
    public bool isEmpty {get; protected set; }

    public Plate()
    {
        tiles = new Tiles(0,0);
        isEmpty = true;
    }
    
    public Plate(int typesCount)
    {
        tiles = new Tiles(typesCount, 0);
        isEmpty = true;
    }

    public Tile[] GetCounts()
    {
        List<Tile> tileList = new List<Tile>();
        for(int i = 0; i < tiles.typesCount; i++)
        {
            Tile current = tiles.GetTiles(i);
            if (current.count != 0)
            {
                tileList.Add(current);
            }
        }

        return tileList.ToArray();
    }
    
    public void PutTiles(Tiles tiles)
    {
        this.tiles = tiles;
        isEmpty = tiles.TotalTiles() == 0;
    }

    public Tile TakeTile(int id)
    {
        Tile outTile =  tiles.GetTiles(id);
        Debug.Assert(outTile.count != 0, "We can't take tile which is not on the plate");
        return outTile;
    }
}

public class CenterPlate : Plate
{
    public bool isFirst { get; protected set; }
    
    public CenterPlate(int typesCount)
    {
        tiles = new Tiles(typesCount, 0);
        isEmpty = true;
    }

    public void AddTiles(Tiles toPut)
    {
        tiles.Union(toPut);
    }
}