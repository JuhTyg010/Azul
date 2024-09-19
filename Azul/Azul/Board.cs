
using System;

namespace Azul {
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
        
        private Tiles trash;
        private bool isGameOver;
        private int nextFirst;

        public Board(int playerCount, string[] playerNames, bool isAdvanced_ = false) {
            Logger.WriteLine(" ");
            Logger.WriteLine("-----------------------------Game start-----------------------------");
            if (playerNames.Length != playerCount) {
                Logger.WriteLine("died cause of incorrect names of players");
                throw new Exception("incorrect number of names, or players");
            }

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
            Logger.WriteLine("Move:");
            Logger.Write("Plate data: ");
            for (int i = 0; i < Plates.Length; i++) {
                Logger.Write($" id: {i} {Plates[i]},");
            }
            Logger.WriteLine($" id: {Plates.Length} {Center}");
            Logger.Write($"Player {Players[CurrentPlayer].name} ({CurrentPlayer}) taking from plate {plateId} tile {tileId} to buffer {bufferId}: ");
            if (Phase != Phase.Taking) {
                Logger.WriteLine("Invalid Phase");
                throw new IllegalOptionException("Invalid Phase");
            }

            if (plateId > Plates.Length) {
                Logger.WriteLine("invalid plate");
                return false;
            }
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
            if (data[tileId].count == 0) {
                Logger.WriteLine("no tiles");
                return false;
            }
        
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
                    Logger.WriteLine("Phase changed to filling");
                    CurrentPlayer = 0;
                    bool isSkiped = false;
                    while (!Players[CurrentPlayer].hasFullBuffer()) {
                        Logger.WriteLine($"Player {Players[CurrentPlayer].name} has no full buffer");
                        if (Players[CurrentPlayer].ClearFloor()) {
                            Logger.WriteLine($"Player {Players[CurrentPlayer].name} will start next turn");
                            nextFirst = CurrentPlayer;
                        }
                        CurrentPlayer++;
                        if (CurrentPlayer == Players.Length) {
                            isSkiped = true;
                            Logger.WriteLine("No player has full buffer");
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
            Logger.WriteLine("Filling:");
            Logger.WriteLine($"Player's data: {Players[CurrentPlayer]}");
            if (Phase != Phase.Placing) {
                Logger.WriteLine("Invalid Phase");
                throw new IllegalOptionException("Invalid Phase");
            }
            int[] fullBuffers = Players[CurrentPlayer].FullBuffers();

            if (col < 0 && isAdvanced) {
                Logger.WriteLine("invalid col");
                return false;
            }
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
                if (Players[CurrentPlayer].ClearFloor()) {
                    Logger.WriteLine($"Player {Players[CurrentPlayer].name} will start next turn");
                    nextFirst = CurrentPlayer;
                }
                CurrentPlayer++;
                while (CurrentPlayer < Players.Length && !Players[CurrentPlayer].hasFullBuffer()) {
                    Logger.WriteLine($"Player {Players[CurrentPlayer].name} has no full buffer");
                    if (Players[CurrentPlayer].ClearFloor()) {
                        Logger.WriteLine($"Player {Players[CurrentPlayer].name} will start next turn");
                        nextFirst = CurrentPlayer;
                    }
                    CurrentPlayer++;
                }

                if (CurrentPlayer == Players.Length) {
                    //All players finished phase 2
                    if (isGameOver) {
                        foreach (var player in Players) player.CalculateBonusPoints();
                        
                        WriteGameOver();

                        Phase = Phase.GameOver;
                    }
                    else {
                        Logger.WriteLine("All players filled to the wall");
                        CurrentPlayer = nextFirst;
                        Phase = Phase.Taking;
                        FillPlates();
                    }
                }
            }
        
            return isFilled;
        }
        
        public void PutToTrash(int typeId, int count) {

            Tiles temp = new Tiles(Globals.TYPE_COUNT, 0);
            temp.PutTile(typeId, count);
            trash.Union(temp);
            Logger.WriteLine($" {count} tiles of type {typeId} put to trash");
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
            if (storage.TotalTiles() < Globals.PLATE_VOLUME * Plates.Length) {
                storage.Union(trash);
            }
            foreach (var plate in Plates) {
                plate.SetTiles(storage.GetRandom(Globals.PLATE_VOLUME));
            }

            Center = new CenterPlate(Globals.TYPE_COUNT);
            fisrtTaken = false;
        }

        private Player[] InitializePlayers(int playerCount, string[] playerNames) {
            var players = new Player[playerCount];

            for (int i = 0; i < playerCount; i++) {
                players[i] = new Player(playerNames[i], this);
                players[i].OnWin += OnWin;
            }

            return players;
        }

        private void WriteGameOver() {
            Logger.WriteLine("-------GGG-------A-------M------M--EEEEEE-------OOO----V-----V----EEEEEE----RRRR------");
            Logger.WriteLine("------G---G-----A-A------MM----MM--E-----------O---O---V-----V----E---------R---R-----");
            Logger.WriteLine("-----G---------A---A-----M-M--M-M--EEEE-------O-----O---V---V-----EEEE------RRRR------");
            Logger.WriteLine("-----G--GGG---AAAAAAA----M--MM--M--EEEE-------O-----O---V---V-----EEEE------R--R------");
            Logger.WriteLine("------G---G---A-----A----M------M--E-----------O---O-----V-V------E---------R---R-----");
            Logger.WriteLine("-------GGG---A-------A---M------M--EEEEEE-------OOO-------V-------EEEEEE----R---R-----");
        }

        private void OnWin(object? sender, EventArgs args) {
            if (Phase == Phase.Taking)
                throw new Exception("Something went really wrong, win cant happen if we are not placing");
            isGameOver = true;
        }
    }
}