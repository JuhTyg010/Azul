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
    public bool fisrtTaken;

    public Board(int playerCount, string[] playerNames) {
        //TODO: check if length of playerNames is same as playerCount
        plates = new Plate[playerCount * 2 + 1];
        players = new Player[playerCount];
        for (int i = 0; i < playerCount; i++) {
            players[i] = new Player(playerNames[i]);
        }
    }
    
    public bool Move(int plateId, int tileId, int bufferId) {   //center is always last
        if (plateId > plates.Length) return false;
        Plate p;
        if (plateId == plates.Length) {
            p = center;
        } else {
            p = plates[plateId];
        }
        var data = p.GetCounts();
        if (data[tileId].count == 0) return false;
        bool success = players[currentPlayer].Place(bufferId, data[tileId]);//TODO: check if its from center for bool

        #region succieded
        
        if (success) {
            p.TakeTile(tileId);
            var newData = p.GetCounts();
            Tiles toPut = new Tiles(newData.Length, 0);
            for (int i = 0; i < newData.Length; i++) {
                toPut.PutTile(newData[i]);
            }
            center.AddTiles(toPut);
            if (ArePlatesEmpty()) {
                currentPlayer = 0;
                while (!players[currentPlayer].hasFullBuffer()) {
                    currentPlayer++;
                    if (currentPlayer == players.Length) {
                        //TODO: skip phase 2
                    }
                }
                phase = 2;
            }
            currentPlayer++;
            if (currentPlayer == players.Length) currentPlayer = 0;
        }
        #endregion
        
        return success;
    }
    
    //TODO: Check if player has some full Buffers
    public bool Calculate(int col) {
        int[] fullBuffers = players[calculating.x].FullBuffers();
        
        bool isFilled = players[calculating.x].Fill(fullBuffers[0], col);
        if (isFilled && fullBuffers.Length == 1) {
            players[calculating.x].ClearFloor();
            calculating.x++;
            while (!players[calculating.x].hasFullBuffer()) {
                calculating.x++;
                if (calculating.x == players.Length) phase = 1;
            } if (calculating.x == players.Length) {
                //All players finished phase 2
                phase = 1;
                //TODO: set currentPlayer based on the first from center point
            }
        }
        
        return isFilled;
    }
    
    

    private bool ArePlatesEmpty() {
        foreach (var plate in plates) {
            if (!plate.isEmpty) {
                return false;
            }
        }

        return center.isEmpty;
    }
}