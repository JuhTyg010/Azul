
namespace Azul;

public class Board
{
    public Player[] Players { get; private set; }
    public Plate[] Plates { get; private set; }
    public CenterPlate Center { get; private set; }
    public Tiles storage { get; private set; }
    public int CurrentPlayer { get; private set; }
    public Phase Phase { get; private set; }
    public Azul.Vector2 calculating; //on x is currently calculated player and on the y is the row we are on
    public int[,] predefinedWall { get; private set; }
    public bool isAdvanced { get; private set; }
    public bool fisrtTaken;

    public Board(int playerCount, string[] playerNames, bool isAdvanced_ = false) {
        //TODO: check if length of playerNames is same as playerCount
        isAdvanced = isAdvanced_;
        Center = new CenterPlate(Globals.TYPE_COUNT);
        storage = new Tiles(Globals.TYPE_COUNT, Globals.TOTAL_TILE_COUNT); 
        
        Plates = new Plate[playerCount * 2 + 1];
        for (int i = 0; i < playerCount * 2 + 1; i++) {
            Plates[i] = new Plate(Globals.TYPE_COUNT);
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
        Phase = Phase.Taking;
        FillPlates();
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
            p.ClearPlate();
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
                Phase = Phase.Placing;
            }
            CurrentPlayer++;
            if (CurrentPlayer == Players.Length) CurrentPlayer = 0;
        }
        #endregion
        
        return success;
    }
    
    public bool Calculate(int col = -1) {
        int[] fullBuffers = Players[calculating.x].FullBuffers();
        if (col < 0 && isAdvanced) return false;
        if (!isAdvanced) {
            for (int tmp = 0; tmp < predefinedWall.GetLength(0); tmp++) {
                if (predefinedWall[fullBuffers[0], tmp] == Players[calculating.x].GetBufferData(fullBuffers[0]).id) {
                    col = tmp;
                    break;
                }
            }
        }
        bool isFilled = Players[calculating.x].Fill(fullBuffers[0], col);
        if (isFilled && fullBuffers.Length == 1) {
            bool isFirst = Players[calculating.x].ClearFloor();
            CurrentPlayer = calculating.x;
            calculating.x++;
            while (calculating.x < Players.Length && !Players[calculating.x].hasFullBuffer()) {
                calculating.x++;
                if (calculating.x == Players.Length) Phase = Phase.Taking;
            } if (calculating.x == Players.Length) {
                //All players finished phase 2
                Phase = Phase.Taking;
                FillPlates();
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

    private void FillPlates() {
        foreach (var plate in Plates) {
            plate.SetTiles(storage.GetRandom(Globals.PLATE_VOLUME));
        }

        fisrtTaken = false;
    }
}