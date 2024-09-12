
namespace Azul;

public class Board
{
    public Player[] Players { get; private set; }
    public Plate[] Plates { get; private set; }
    public CenterPlate Center { get; private set; }
    public Tiles storage { get; private set; }
    public int CurrentPlayer { get; private set; }
    public Phase Phase { get; private set; }
    public int[,] predefinedWall { get; private set; }
    public bool isAdvanced { get; private set; }
    public bool fisrtTaken;
    private bool isGameOver;
    private int nextFirst;

    public Board(int playerCount, string[] playerNames, bool isAdvanced_ = false) {
        //TODO: check if length of playerNames is same as playerCount
        isAdvanced = isAdvanced_;
        Center = new CenterPlate(Globals.TYPE_COUNT);
        storage = new Tiles(Globals.TYPE_COUNT, Globals.TOTAL_TILE_COUNT); 
        
        Plates = new Plate[playerCount * 2 + 1];
        for (int i = 0; i < playerCount * 2 + 1; i++) {
            Plates[i] = new Plate(Globals.TYPE_COUNT);
        }

        Players = InitializePlayers(playerCount, playerNames);

        predefinedWall = new int[Globals.WALL_DIMENSION, Globals.WALL_DIMENSION];
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                predefinedWall[j % Globals.WALL_DIMENSION, (i + j) % Globals.WALL_DIMENSION] = i;
            }
        }

        CurrentPlayer = 0;
        Phase = Phase.Taking;
        FillPlates();
    }
    
    public bool Move(int plateId, int tileId, int bufferId) {   //center is always last
        if(Phase != Phase.Taking) throw new IllegalOptionException("Invalid Phase");
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
                bool isSkiped = false;
                while (!Players[CurrentPlayer].hasFullBuffer()) {
                    if(Players[CurrentPlayer].ClearFloor()) nextFirst = CurrentPlayer;
                    CurrentPlayer++;
                    if (CurrentPlayer == Players.Length) {
                        isSkiped = true;
                        CurrentPlayer = nextFirst;
                    }
                }

                if (!isSkiped) Phase = Phase.Placing;

            }
            else {
                CurrentPlayer++;
                if (CurrentPlayer == Players.Length) CurrentPlayer = 0;
            }
        }

        #endregion
        
        return success;
    }
    
    public bool Calculate(int col = Globals.EMPTY_CELL) {
        if(Phase != Phase.Placing) throw new IllegalOptionException("Invalid Phase");
        int[] fullBuffers = Players[CurrentPlayer].FullBuffers();
        if (col < 0 && isAdvanced) return false;
        if (!isAdvanced) {
            for (int tmp = 0; tmp < predefinedWall.GetLength(0); tmp++) {
                if (predefinedWall[fullBuffers[0], tmp] == Players[CurrentPlayer].GetBufferData(fullBuffers[0]).id) {
                    col = tmp;
                    break;
                }
            }
        }
        bool isFilled = Players[CurrentPlayer].Fill(fullBuffers[0], col);
        if (isFilled && fullBuffers.Length == 1) {
            if(Players[CurrentPlayer].ClearFloor()) nextFirst = CurrentPlayer;
            CurrentPlayer++;
            while (CurrentPlayer < Players.Length && !Players[CurrentPlayer].hasFullBuffer()) {
                if(Players[CurrentPlayer].ClearFloor()) nextFirst = CurrentPlayer;
                CurrentPlayer++;
                if (CurrentPlayer == Players.Length) {
                    CurrentPlayer = nextFirst;
                    Phase = Phase.Taking;
                }
            }

            if (CurrentPlayer == Players.Length) {
                //All players finished phase 2
                if (isGameOver) Phase = Phase.GameOver; //TODO: calculate bonuses, I guess
                else {
                    CurrentPlayer = nextFirst;
                    Phase = Phase.Taking;
                    FillPlates();
                }
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

    private Player[] InitializePlayers(int playerCount, string[] playerNames) {
        var players = new Player[playerCount];
        for (int i = 0; i < playerCount; i++) {
            players[i] = new Player(playerNames[i]);
            players[i].OnWin += OnWin;
        }
        return players;
    }

    private void OnWin(object? sender, EventArgs args) {
        if (Phase == Phase.Taking)
            throw new Exception("Something went really wrong, win cant happen if we are not placing");
        isGameOver = true;
    }
}