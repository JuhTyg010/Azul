using System.Numerics;

namespace AzulCLI;

public class Board
{
    public Player[] players { get; private set; }
    public Plate[] plates { get; private set; }
    public CenterPlate center { get; private set; }
    public Tiles storage { get; private set; }
    public int currentPlayer { get; private set; }
    public int phase { get; private set; }
    public AzulCLI.Vector2 calculating; //on x is currently calculated player and on the y is the row we are on
    public int[,] predefinedWall { get; private set; }
    private bool isAdvanced;

    public bool Move(int plateId, int tileId, int bufferId) //center is always last
    {
        if (plateId > plates.Length) return false;
        Plate p;
        if (plateId == plates.Length)
        {
            p = center;
        }
        else
        {
            p = plates[plateId];
        }

        var data = p.GetCounts();
        if (data[tileId].count == 0) return false;
        bool success = players[currentPlayer].Place(bufferId, data[tileId]);//TODO: check if its from center for bool
        if (success)
        {
            p.TakeTile(tileId);
            var newData = p.GetCounts();
            Tiles toPut = new Tiles(newData.Length, 0);
            for (int i = 0; i < newData.Length; i++)
            {
                toPut.PutTile(newData[i]);
            }
            center.AddTiles(toPut);
            currentPlayer++;
            if (currentPlayer == players.Length) currentPlayer = 0;
        }

        return success;

    }

    public bool Calculate(int col)
    {
        int[] fullBuffers = players[calculating.x].FullBuffers();
        //TODO: get first => calculating.y
        //TODO: Try to fill it
        //TODO: Move to next buffer, or stay if failed, aslo is=f success calculate points
        return false;
    }
}