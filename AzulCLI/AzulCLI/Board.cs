
namespace AzulCLI;

public class Board
{
    public Player[] Players { get; private set; }
    public Plate[] Plates { get; private set; }
    public CenterPlate Center { get; private set; }
    public Tiles storage { get; private set; }
    public int CurrentPlayer { get; private set; }
    public int Phase { get; private set; }
    public AzulCLI.Vector2 calculating; //on x is currently calculated player and on the y is the row we are on
    public int[,] predefinedWall { get; private set; }
    private bool isAdvanced;
    public bool fisrtTaken;

    public Board(int playerCount, string[] playerNames) {
        //TODO: check if length of playerNames is same as playerCount
        Center = new CenterPlate();
        storage = new Tiles(Globals.TYPE_COUNT, Globals.TOTAL_TILE_COUNT); 
        
        Plates = new Plate[playerCount * 2 + 1];
        for (int i = 0; i < playerCount * 2 + 1; i++) {
            Plates[i] = new Plate();
        }
        
        Players = new Player[playerCount];
        for (int i = 0; i < playerCount; i++) {
            Players[i] = new Player(playerNames[i]);
        }

        predefinedWall = new int[Globals.TYPE_COUNT, Globals.TYPE_COUNT];
        for (int i = 0; i < Globals.TYPE_COUNT; i++) {
            for (int j = 0; j < Globals.TYPE_COUNT; j++) {
                predefinedWall[j % Globals.TYPE_COUNT, (i + j) % Globals.TYPE_COUNT] = i;
            }
        }

        CurrentPlayer = 0;
    }
    
    public bool Move(int plateId, int tileId, int bufferId) {   //center is always last
        if (plateId > Plates.Length) return false;
        Plate p;
        bool isFirstInCenter = false;
        if (plateId == Plates.Length) {
            p = Center;
            if (!fisrtTaken) {
                isFirstInCenter = true;
                fisrtTaken = true;
            }
        } else {
            p = Plates[plateId];
        }
        var data = p.GetCounts();
        if (data[tileId].count == 0) return false;
        
        bool success = Players[CurrentPlayer].Place(bufferId, data[tileId], isFirstInCenter);

        #region succieded
        
        if (success) {
            p.TakeTile(tileId);
            var newData = p.GetCounts();
            Tiles toPut = new Tiles(newData.Length, 0);
            for (int i = 0; i < newData.Length; i++) {
                toPut.PutTile(newData[i]);
            }
            Center.AddTiles(toPut);
            if (ArePlatesEmpty()) {
                CurrentPlayer = 0;
                while (!Players[CurrentPlayer].hasFullBuffer()) {
                    CurrentPlayer++;
                    if (CurrentPlayer == Players.Length) {
                        //TODO: skip phase 2
                    }
                }
                Phase = 2;
            }
            CurrentPlayer++;
            if (CurrentPlayer == Players.Length) CurrentPlayer = 0;
        }
        #endregion
        
        return success;
    }
    
    public bool Calculate(int col) {
        int[] fullBuffers = Players[calculating.x].FullBuffers();
        
        bool isFilled = Players[calculating.x].Fill(fullBuffers[0], col);
        if (isFilled && fullBuffers.Length == 1) {
            bool isFirst = Players[calculating.x].ClearFloor();
            CurrentPlayer = calculating.x;
            calculating.x++;
            while (!Players[calculating.x].hasFullBuffer()) {
                calculating.x++;
                if (calculating.x == Players.Length) Phase = 1;
            } if (calculating.x == Players.Length) {
                //All players finished phase 2
                Phase = 1;
                fisrtTaken = false;
                
            }
        }
        
        return isFilled;
    }
    
    

    private bool ArePlatesEmpty() {
        foreach (var plate in Plates) {
            if (!plate.isEmpty) {
                return false;
            }
        }

        return Center.isEmpty;
    }
}