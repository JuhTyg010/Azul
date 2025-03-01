using System;

namespace Azul {
    public class Board {
        public Player[] Players { get; private set; }
        public Plate[] Plates { get; private set; }
        public CenterPlate Center { get; private set; }
        public Tiles storage { get; private set; }
        public int CurrentPlayer { get; private set; }
        public Phase Phase { get; private set; }
        public int[,] predefinedWall { get; private set; }
        public bool isAdvanced { get; private set; }
        public bool fisrtTaken;
        
        public event EventHandler<MyEventArgs> NextTakingMove;
        public event EventHandler<MyEventArgs> NextPlacingMove;
        
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
            trash = new Tiles(Globals.TYPE_COUNT, 0);
        
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

        public void StartGame() {
            NextMove();
        }
        public bool CanMove(int plateId, int typeId, int bufferId) {
            if(plateId < 0 || plateId > Plates.Length) return false;
            Plate p;
            if (plateId == Plates.Length) {
                p = Center;
            } else {
                p = Plates[plateId];
            } 
            if (p.TileCountOfType(typeId) == 0) return false;

            return Players[CurrentPlayer].CanPlace(bufferId, typeId);
        }

        public bool CanMove(Move move) {
            return CanMove(move.plateId, move.tileId, move.bufferId);
        }
        
        public bool Move(int plateId, int tileId, int bufferId) {   //center is always last

            StateLogData(plateId, tileId, bufferId);

            if (!CanMove(plateId, tileId, bufferId)) return false;
            
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
        
            Players[CurrentPlayer].Place(bufferId, data[tileId], isFirstInCenter); //is always true cause of CanMove
            
            p.TakeTile(tileId);
            var newData = p.GetCounts();
            p.ClearPlate();
            Tiles toPut = new Tiles(newData.Length, 0);
            for (int i = 0; i < newData.Length; i++) {
                toPut.PutTile(newData[i]);
            }

            Center.AddTiles(toPut);
            NextMove();
            return true;
        }

        public bool Move(Move move) {
            return Move(move.plateId, move.tileId, move.bufferId);
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
                col = FindColInRow(fullBuffers[0], Players[CurrentPlayer].GetBufferData(fullBuffers[0]).id);
            }
            bool isFilled = Players[CurrentPlayer].Fill(fullBuffers[0], col);
            
            NextMove();
            
            return isFilled;
        }
        
        public void PutToTrash(int typeId, int count) {

            Tiles temp = new Tiles(Globals.TYPE_COUNT, 0);
            temp.PutTile(typeId, count);
            trash.Union(temp);
            Logger.WriteLine($" {count} tiles of type {typeId} put to trash");
        }

        public Move[] GetValidMoves() {
            List<Move> validMoves = new List<Move>();
            for (int i = 0; i < Globals.TYPE_COUNT; i++) {
                int[] buffers = GetValidBufferIds(i);
                int[] plates = GetValidPlateIds(i);
                foreach (int buffer in buffers) {
                    foreach (var plate in plates) {
                        validMoves.Add(new Move(i, plate, buffer));
                    }
                }
            }
            return validMoves.ToArray();
        }
        
        public double[] EncodeBoardState(int stateSize, int id, bool othersData = true) {
            double[] state = new double[stateSize];
            if(stateSize < 87) throw new IllegalOptionException("To small state size");

            //for plates takes max 45
            for (int i = 0; i < Plates.Length; i++) {
                for (int j = 0; j < Globals.TYPE_COUNT; j++) {
                    state[(i * Globals.TYPE_COUNT) + j] = Plates[i].TileCountOfType(j);
                }
            }
            // center plate
            for (int i = 0; i < Globals.TYPE_COUNT; i++) {
                state[45 + i] = Center.TileCountOfType(i);
            }
            state[50] = Center.isFirst ? 0 : 1;

            int index = 51;
            index = AddPlayerData(index, state, Players[id]);
            if (othersData) {
                foreach (Player p in Players) {
                    if (p != Players[id]) {
                        index = AddPlayerData(index, state, p);
                    }
                }
            }

            return state;
        }

        private int AddPlayerData(int startIndex, double[] data, Player player) {
            //buffers
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                var buffer = player.GetBufferData(i);
                data[startIndex] = buffer.id;
                startIndex++;
                data[startIndex] = buffer.count;
                startIndex++;
            }
            //floor
            data[startIndex] = player.floor.Count;
            startIndex++;
            data[startIndex] = player.isFirst ? 0 : 1;
            //wall
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    data[startIndex] = player.wall[i, j];
                    startIndex++;
                }
            }
            return startIndex;
        }

        public double[] GetNextState(double[] state, Move move, int id) {
            double[] nextState = (double[]) state.Clone();

            int countOfTiles = 0;
            //remove from plate
            if (move.plateId != Plates.Length) {
                //clear the plate
                for (int i = 0; i < Globals.TYPE_COUNT; i++) {
                    nextState[(move.plateId * Globals.TYPE_COUNT) + i] = 0;
                }
                //add rest to center
                for (int i = 0; i < Globals.TYPE_COUNT; i++) {
                    if (i == move.tileId) {
                        countOfTiles = (int) state[move.plateId * Globals.TYPE_COUNT + i];
                    } else nextState[45 + i] = state[move.plateId * Globals.TYPE_COUNT + i];
                }
            }
            else {
                countOfTiles = (int) nextState[45 + move.tileId];
                nextState[45 + move.tileId] = 0;
                nextState[50] = 0;
            }
            
            //edit player data
            int index = 51;
            int floorIndex = index + 5;
            if (move.bufferId != Globals.WALL_DIMENSION) {
                int inBuffer = Players[id].GetBufferData(move.bufferId).count;
                int capacity = move.bufferId + 1;
                int afterFilling = Math.Min(inBuffer + countOfTiles, capacity);
                nextState[index + move.bufferId] = afterFilling;
                if (inBuffer + countOfTiles > capacity) {
                    int toFloor = capacity - (inBuffer + countOfTiles);
                    nextState[floorIndex] = Math.Min(Globals.FLOOR_SIZE, nextState[floorIndex] + toFloor);
                }
            }
            else {
                nextState[floorIndex] = Math.Min(Globals.FLOOR_SIZE, nextState[floorIndex] + countOfTiles);
            }

            nextState[floorIndex + 1] = (int) nextState[50] == (int) state[50] ? 1 : 0;
            return nextState;
        }

        public int FindColInRow(int row, int typeId) {
            if (isAdvanced) throw new IllegalOptionException("there is no predefined wall in advanced mode");
            if(row < 0 || row >= Globals.WALL_DIMENSION) return Globals.EMPTY_CELL;
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if(predefinedWall[row,i] == typeId) return i;
            }
            return Globals.EMPTY_CELL;
        }

        private bool NextWithFullBuffer() {
            
            while (!Players[CurrentPlayer].hasFullBuffer()) {
                Logger.WriteLine($"Player {Players[CurrentPlayer].name} has no full buffer");
                if (Players[CurrentPlayer].ClearFloor()) {
                    Logger.WriteLine($"Player {Players[CurrentPlayer].name} will start next turn");
                    nextFirst = CurrentPlayer;
                }
                CurrentPlayer++;
                if (CurrentPlayer == Players.Length) {
                    return false;
                }
            }
            return true;
        }

        private void StartNextTurn() {
            Logger.WriteLine("Starting next turn");
            Phase = Phase.Taking;
            CurrentPlayer = nextFirst;
            FillPlates();
        }

        private int[] GetValidBufferIds(int typeId) {
            if(typeId < 0 || typeId >= Globals.TYPE_COUNT) throw new IllegalOptionException("Invalid type");
            List<int> bufferIds = new List<int>();
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if (Players[CurrentPlayer].CanPlace(i, typeId)) {
                    bufferIds.Add(i);
                }
            }
            bufferIds.Add(Globals.WALL_DIMENSION);
            return bufferIds.ToArray();
        }

        private void StateLogData(int plateId, int tileId, int bufferId) {
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
                //return false;
            }
        }

        private int[] GetValidPlateIds(int typeId) {
            List<int> plateIds = new List<int>();
            for (int i = 0; i < Plates.Length; i++) {
                if (Plates[i].TileCountOfType(typeId) != 0) {
                    plateIds.Add(i);
                }
            }

            if (Center.TileCountOfType(typeId) != 0) {
                plateIds.Add(Plates.Length);
            }
            return plateIds.ToArray();
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
                Console.WriteLine("trash to storage");
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

        private void NextMove() {
            if (Phase == Phase.Taking) {
                if (ArePlatesEmpty()) {
                    Logger.WriteLine("Plates are empty, starting filling");
                    CurrentPlayer = 0;
                    if (NextWithFullBuffer()) {
                        Logger.WriteLine($"Found full buffer player: {Players[CurrentPlayer].name}");
                        Phase = Phase.Placing;
                        OnNextPlacingMove(new MyEventArgs(CurrentPlayer, this));
                    }
                    else {
                        Logger.WriteLine("No player has full buffer");
                        StartNextTurn();
                        OnNextTakingMove(new MyEventArgs(CurrentPlayer, this));
                    }
                }
                else {
                    CurrentPlayer++;
                    CurrentPlayer %= Players.Length;
                    Logger.WriteLine($"Player's {Players[CurrentPlayer].name} move");
                    OnNextTakingMove(new MyEventArgs(CurrentPlayer, this));
                }
            } else if (Phase == Phase.Placing) {
                if (NextWithFullBuffer()) {
                    OnNextPlacingMove(new MyEventArgs(CurrentPlayer,this));
                }
                else {
                    Logger.WriteLine("No player has full buffer");
                    if (isGameOver) {
                        foreach (var player in Players) player.CalculateBonusPoints();
                        WriteGameOver();
                        Phase = Phase.GameOver;
                    }
                    else {
                        Logger.WriteLine("All players filled to the wall");
                        StartNextTurn();
                        OnNextTakingMove(new MyEventArgs(CurrentPlayer, this));
                    }
                }
            }
        }  
        protected virtual void OnNextTakingMove(MyEventArgs e) {
            NextTakingMove?.Invoke(this, e);  
        }

        protected virtual void OnNextPlacingMove(MyEventArgs e) {
            NextPlacingMove?.Invoke(this, e);
        }
    }
    
    public class MyEventArgs : EventArgs {  
        public int playerId;
        public Board board;

        public MyEventArgs(int playerId, Board board) {
            this.playerId = playerId;
            this.board = board;
        }
    }
}