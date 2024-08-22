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

    public Tile[] GetCounts() {
        Tile[] tile = new Tile[this.tiles.typesCount];
        for(int i = 0; i < tiles.typesCount; i++) {
            tile[i] = new Tile(i,tiles.TileCountOfType(i));
        }

        return tile;
    }

    public int TileCountOfType(int typeId) {
        return tiles.TileCountOfType(typeId);
    }
    
    public void SetTiles(Tiles tiles) {
        this.tiles = tiles;
        isEmpty = tiles.TotalTiles() == 0;
    }

    public Tile TakeTile(int id) {
        Tile outTile =  tiles.GetTiles(id);
        Debug.Assert(outTile.count != 0, "We can't take tile which is not on the plate");
        isEmpty = tiles.TotalTiles() == 0;
        return outTile;
    }
}

public class CenterPlate : Plate {
    public bool isFirst { get; protected set; }
    
    public CenterPlate(int typesCount) {
        tiles = new Tiles(typesCount, 0);
        isEmpty = true;
    }

    public void AddTiles(Tiles toPut) {
        tiles.Union(toPut);
        isEmpty = tiles.TotalTiles() == 0;
    }
}