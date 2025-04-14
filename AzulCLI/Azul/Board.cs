using System;
using System.Collections.Generic;

namespace Azul {
    
    /// <summary>
    /// Represent access point between Game library and frontend
    /// </summary>
    public class Board {

        private const int EncodeBase = 21;
        
        public Player[] Players { get; private set; }
        public Plate[] Plates { get; private set; }
        public CenterPlate Center { get; private set; }
        public Tiles Storage { get; private set; }
        public int CurrentPlayer { get; private set; }
        public Phase Phase { get; private set; }
        public int[,] PredefinedWall { get; private set; }
        public bool IsAdvanced { get; private set; }
        public bool FisrtTaken;
        
        public event EventHandler<MyEventArgs> NextTakingMove;
        public event EventHandler<MyEventArgs> NextPlacingMove;
        
        private Tiles _trash;
        private bool _isGameOver;
        private int _nextFirst;
        private string[] _playerNames;

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other"> other Board class to be copied</param>
        private Board(Board other) {
            Center = other.Center;
            Players = other.Players;
            Plates = other.Plates;
            Storage = other.Storage;
            CurrentPlayer = other.CurrentPlayer;
            Phase = other.Phase;
            PredefinedWall = other.PredefinedWall;
            IsAdvanced = other.IsAdvanced;
            FisrtTaken = other.FisrtTaken;
            _trash = other._trash;
            _isGameOver = other._isGameOver;
            _nextFirst = other._nextFirst;
            _playerNames = other._playerNames;

        }
        
        /// <summary>
        /// Default constructor, used to initialize a game
        /// </summary>
        /// <param name="playerCount"> Specifies number of players</param>
        /// <param name="playerNames"> Names to be assigned to the players</param>
        /// <param name="isAdvanced"> Mode of the game false  for basec</param>
        /// <param name="fileName"> Name of the file where are logs added</param>
        /// <exception cref="Exception"></exception>
        public Board(int playerCount, string[] playerNames, bool isAdvanced = false, string fileName = "azul_log.txt") {
            IsAdvanced = isAdvanced;
            this._playerNames = playerNames;
            Logger.SetName(fileName);
            
            if (playerNames.Length != playerCount) {
                Logger.WriteLine("died cause of incorrect names of players");
                throw new Exception("incorrect number of names, or players");
            }
            
            Plates = new Plate[playerCount * 2 + 1];
            for (int i = 0; i < playerCount * 2 + 1; i++) {
                Plates[i] = new Plate(Globals.TYPE_COUNT);
            }

        }

        /// <summary>
        /// Reinitialize the game to the beginning, restarts all the structs
        /// </summary>
        public void StartGame() {
            
            Logger.WriteLine(" ");
            Logger.WriteLine("-----------------------------Game start-----------------------------");

            Center = new CenterPlate(Globals.TYPE_COUNT);
            Storage = new Tiles(Globals.TYPE_COUNT, Globals.TOTAL_TILE_COUNT);
            _trash = new Tiles(Globals.TYPE_COUNT, 0);
            
            Players = InitializePlayers(_playerNames.Length, _playerNames);

            PredefinedWall = new int[Globals.WALL_DIMENSION, Globals.WALL_DIMENSION];
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    PredefinedWall[j % Globals.WALL_DIMENSION, (i + j) % Globals.WALL_DIMENSION] = i;
                }
            }

            CurrentPlayer = 0;
            Phase = Phase.Taking;
            _isGameOver = false;
            FillPlates();
            NextMove();
        }
        
        
        /// <summary>
        /// Method to recognize if some move is playable
        /// </summary>
        /// <param name="plateId"> Id of the plate we are taking from</param>
        /// <param name="typeId"> Id of the type we are taking from plate</param>
        /// <param name="bufferId">Id of the buffer where to put the tiles</param>
        /// <returns>if it's possible to do move from input</returns>
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

        /// <summary>
        /// Method to recognize if some move is playable
        /// </summary>
        /// <param name="move"> Struct with parameters plateId, typeId, bufferId</param>
        /// <returns>if it's possible to do move from input</returns>
        public bool CanMove(Move move) {
            return CanMove(move.plateId, move.tileId, move.bufferId);
        }
        
        /// <summary>
        /// Method which manage some move
        /// </summary>
        /// <param name="plateId"> Id of the plate we are taking from</param>
        /// <param name="typeId"> Id of the type we are taking from plate</param>
        /// <param name="bufferId">Id of the buffer where to put the tiles</param>
        /// <returns>if it's possible to do move from input</returns>
        public bool Move(int plateId, int tileId, int bufferId) {   //center is always last

            StateLogData(plateId, tileId, bufferId);

            if (!CanMove(plateId, tileId, bufferId)) return false;
            
            Plate p;
            bool isFirstInCenter = false;
            if (plateId == Plates.Length) {
                p = Center;
                if (!FisrtTaken) {
                    isFirstInCenter = true;
                    FisrtTaken = true;
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

        /// <summary>
        /// Method which manage some move
        /// </summary>
        /// <param name="move"> Struct with parameters plateId, typeId, bufferId</param>
        /// <returns>if it's possible to do move from input</returns>
        public bool Move(Move move) {
            return Move(move.plateId, move.tileId, move.bufferId);
        }
    
        /// <summary>
        /// Method used to calculate addition to players wall
        /// </summary>
        /// <param name="col">in advanced game specifying column chosen for tile from buffer</param>
        /// <returns> in advanced game returns if it was possible</returns>
        /// <exception cref="IllegalOptionException"> If called in other phase than placing</exception>
        public bool Calculate(int col = Globals.EMPTY_CELL) {
            Logger.WriteLine("Filling:");
            Logger.WriteLine($"Player's data: {Players[CurrentPlayer]}");
            if (Phase != Phase.Placing) {
                Logger.WriteLine("Invalid Phase");
                throw new IllegalOptionException("Invalid Phase");
            }
            int[] fullBuffers = Players[CurrentPlayer].FullBuffers();

            if (col < 0 && IsAdvanced) {
                Logger.WriteLine("invalid col");
                return false;
            }
            if (!IsAdvanced) {
                col = FindColInRow(fullBuffers[0], Players[CurrentPlayer].GetBufferData(fullBuffers[0]).id);
            }
            bool isFilled = Players[CurrentPlayer].Fill(fullBuffers[0], col);
            
            NextMove();
            
            return isFilled;
        }
        
        internal void PutToTrash(int typeId, int count) {

            Tiles temp = new Tiles(Globals.TYPE_COUNT, 0);
            temp.PutTile(typeId, count);
            _trash.Union(temp);
            Logger.WriteLine($" {count} tiles of type {typeId} put to trash");
        }

        /// <summary>
        /// This method handles for frontend list of possible moves (useful for AI training)
        /// </summary>
        /// <returns> array of possible moves</returns>
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

        /// <summary>
        /// This method transforms state to array of doubles for AI training
        /// </summary>
        /// <param name="id"> id of player from which perspective it is</param>
        /// <returns> array of doubles representing game state</returns>
        public double[] EncodeBoardState(int id) {
            int stateSize = 59;    //59 -> all data
            double[] state = new double[stateSize];

            //for plates takes max 10
            for (int i = 0; i < Plates.Length; i++) {
                state[i] = EncodePlateData(Plates[i]);
            }
            // center plate
            state[9] = EncodePlateData(Center);
            state[10] = Center.isFirst ? 0 : 1;

            int index = 11;
            index = AddPlayerData(index, state, Players[id]);
            
            foreach (Player p in Players) {
                if (p != Players[id]) {
                    index = AddPlayerData(index, state, p);
                }
            }
            return state;
        }

        /**
         * encodes player to 12 numbers
         */
        private int AddPlayerData(int startIndex, double[] data, Player player) {
            //buffers
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                var buffer = player.GetBufferData(i);
                data[startIndex] = EncodeBufferData(buffer);
                startIndex++;
            }
            
            //floor
            data[startIndex] = player.floor.Count;
            startIndex++;
            data[startIndex] = player.isFirst ? 0 : 1;
            startIndex++;
            
            //wall
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                double value = 0;
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    value += player.wall[i,j] != Globals.EMPTY_CELL ? Math.Pow(EncodeBase, j) : 0;
                }
                data[startIndex] = value;
                startIndex++;
            }
            return startIndex;
        }

        /// <summary>
        /// This method shows how the board would look like if move was made in state
        /// </summary>
        /// <param name="state">State from method calculates</param>
        /// <param name="move">Move that is applied to the move</param>
        /// <returns> Array of doubles representing state after move </returns>
        public double[] GetNextState(double[] state, Move move) {
            double[] nextState = (double[]) state.Clone();

            int countOfTiles = 0;
            //remove from plate
            if (move.plateId != Plates.Length) {
                
                //clear the plate
                nextState[move.plateId] = 0;
                //add rest to center
                var centerBefore = DecodePlateData((int)state[9]);
                var oldPlateData = DecodePlateData((int)state[move.plateId]);
                countOfTiles = oldPlateData[move.tileId];
                oldPlateData[move.tileId] = 0;
                
                var newCenter = new int[centerBefore.Length];
                for (int i = 0; i < newCenter.Length; i++) {
                    newCenter[i] = centerBefore[i] + oldPlateData[i];
                }
                nextState[9] = EncodePlateData(newCenter);

            }
            else {
                var centerPlateData = DecodePlateData((int)state[9]);
                countOfTiles = centerPlateData[move.tileId];
                centerPlateData[move.tileId] = 0;
                nextState[9] = EncodePlateData(centerPlateData);
                nextState[10] = 0;  //if is first: 1->0 else: 0->0
            }
            
            //edit player data
            int index = 11;
            int floorIndex = index + 5;
            if (move.bufferId != Globals.WALL_DIMENSION) {
                int inBuffer = DecodeBufferData((int) state[index + move.bufferId])[1];
                int capacity = move.bufferId + 1;
                int afterFilling = Math.Min(inBuffer + countOfTiles, capacity);
                int newId = inBuffer == 0 ? move.tileId : DecodeBufferData((int) state[index + move.bufferId])[0];
                nextState[index + move.bufferId] = EncodeBufferData(new int[] { newId, afterFilling });
                if (inBuffer + countOfTiles > capacity) {
                    int toFloor = (inBuffer + countOfTiles) - capacity;
                    nextState[floorIndex] = Math.Min(Globals.FLOOR_SIZE, state[floorIndex] + toFloor);
                }
            }
            else {
                nextState[floorIndex] = Math.Min(Globals.FLOOR_SIZE, state[floorIndex] + countOfTiles);
            }

            nextState[floorIndex + 1] = (int) nextState[10] == (int) state[10] ? 1 : 0;
            return nextState;
        }

        /// <summary>
        /// This method helps to find in witch column of the row will be tile of specific type
        /// </summary>
        /// <param name="row"> row of the wall</param>
        /// <param name="typeId">id of tile type</param>
        /// <returns>index of the column</returns>
        /// <exception cref="IllegalOptionException">In advanced game it's not predefined</exception>
        public int FindColInRow(int row, int typeId) {
            if (IsAdvanced) throw new IllegalOptionException("there is no predefined wall in advanced mode");
            if(row < 0 || row >= Globals.WALL_DIMENSION) return Globals.EMPTY_CELL;
            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                if(PredefinedWall[row,i] == typeId) return i;
            }
            return Globals.EMPTY_CELL;
        }

        /// <summary>
        /// This method transforms data of all plates to numeric array
        /// </summary>
        /// <returns>array of ints representing transformed data</returns>
        public int[] GetPlatesData() {
            int[] plates = new int[Plates.Length + 1];
            for (int i = 0; i < Plates.Length; i++) {
                plates[i] = EncodePlateData(Plates[i]);
            }
            plates[Plates.Length] = EncodePlateData(Center);
            
            return plates;
        }

        /// <summary>
        /// This method encodes data of the plate (tiles on it) to one value
        /// </summary>
        /// <param name="plate"> specifies which plate to encode</param>
        /// <returns>int representing plate data</returns>
        public int EncodePlateData(Plate plate) {
            var data = plate.GetCounts();
            int[] arrData = new int[data.Length];
            foreach (var tile in data) {
                arrData[tile.id] = tile.count;
            }
            return EncodePlateData(arrData);
        }

        /// <summary>
        /// This method encodes data of the plate (tiles on it) to one value
        /// </summary>
        /// <param name="plateData"> specifies tiles on the plate</param>
        /// <returns>int representing plate data</returns>
        public int EncodePlateData(int[] plateData) {
            int encoded = 0;
            for (int i = 0; i < plateData.Length; i++) {
                encoded += (int) Math.Pow(EncodeBase, i) * plateData[i];
            }
            return encoded;
        }
        
        /// <summary>
        /// This method encodes buffer data to one value
        /// </summary>
        /// <param name="tile"> tile representing buffer data</param>
        /// <returns>int representing specific buffer data</returns>
        public int EncodeBufferData(Tile tile) {
            return EncodeBufferData(new int[] {tile.id , tile.count});
        }

        /// <summary>
        /// This method encodes buffer data to one value
        /// </summary>
        /// <param name="arrData"> array representing data of the buffer</param>
        /// <returns>int representing specific buffer data</returns>
        public int EncodeBufferData(int[] arrData) {
            return arrData[0] + 1 + (arrData[1] + 1) * 6;
        }

        /// <summary>
        /// This method decodes buffer data from one value
        /// </summary>
        /// <param name="encoded"> value representing data</param>
        /// <returns>array of ints representing buffer data</returns>
        public int[] DecodeBufferData(int encoded) {
            int first = (encoded % 6) - 1;
            int second = (encoded / 6) - 1;
            return new int[] {first, second};
        }

        /// <summary>
        /// This method decodes plate data from one value
        /// </summary>
        /// <param name="encoded">value representing specific plate data </param>
        /// <returns>array of ints representing tiles and ich count on the plate</returns>
        public int[] DecodePlateData(int encoded) {
            int[] arr = new int[Globals.TYPE_COUNT];
            for (int i = 0; i < Globals.TYPE_COUNT; i++) {
                arr[i] = encoded % EncodeBase;
                encoded /= EncodeBase;
            }
            return arr;
        }

        /// <summary>
        /// This method finds next player which has full buffer
        /// </summary>
        /// <returns>true if there is someone with full buffer</returns>
        private bool NextWithFullBuffer() {
            
            while (!Players[CurrentPlayer].hasFullBuffer()) {
                Logger.WriteLine($"Player {Players[CurrentPlayer].name} has no full buffer");
                if (Players[CurrentPlayer].ClearFloor()) {
                    Logger.WriteLine($"Player {Players[CurrentPlayer].name} will start next turn");
                    _nextFirst = CurrentPlayer;
                }
                CurrentPlayer++;
                if (CurrentPlayer == Players.Length) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// This method is prep method for start taking phase
        /// </summary>
        private void StartNextTakingPhase() {
            Logger.WriteLine("Starting next turn");
            Phase = Phase.Taking;
            CurrentPlayer = _nextFirst;
            FillPlates();
        }

        /// <summary>
        /// This method finds ids of buffers where current player can place tile with type id
        /// </summary>
        /// <param name="typeId">tile type to check for</param>
        /// <returns>array of ints representing ids of buffers</returns>
        /// <exception cref="IllegalOptionException"> type id in range check</exception>
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
            Logger.WriteLine("Plate data: ");
            int i = 0;
            for (; i <= Plates.Length / 2; i++) Logger.Write($" id: {i} {Plates[i]},");
            Logger.WriteLine("");
            for(;i < Plates.Length; i++) Logger.Write($" id: {i} {Plates[i]}");
            
            Logger.WriteLine($" id: {Plates.Length} {Center}");
            Logger.Write($"Player {Players[CurrentPlayer].name} ({CurrentPlayer}) plate: {plateId} tile: {tileId} buffer: {bufferId}: ");
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
            if (Storage.TotalTiles() < Globals.PLATE_VOLUME * Plates.Length) {
                Console.WriteLine("trash to storage");
                Storage.Union(_trash);
            }
            foreach (var plate in Plates) {
                plate.SetTiles(Storage.GetRandom(Globals.PLATE_VOLUME));
            }

            Center = new CenterPlate(Globals.TYPE_COUNT);
            FisrtTaken = false;
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
            foreach (var player in Players) {   
                Logger.WriteLine($"Player {player.name}: points: {player.pointCount}");
            }
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
            _isGameOver = true;
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
                        StartNextTakingPhase();
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
                    if (_isGameOver) {
                        foreach (var player in Players) player.CalculateBonusPoints();
                        WriteGameOver();
                        Phase = Phase.GameOver;
                    }
                    else {
                        Logger.WriteLine("All players filled to the wall");
                        StartNextTakingPhase();
                        OnNextTakingMove(new MyEventArgs(CurrentPlayer, this));
                    }
                }
            }
        }

        private void WritePlayerData() {
            
        }
        
        protected virtual void OnNextTakingMove(MyEventArgs e) {
            NextTakingMove?.Invoke(this, e);  
        }

        protected virtual void OnNextPlacingMove(MyEventArgs e) {
            NextPlacingMove?.Invoke(this, e);
        }
    }
    
    public class MyEventArgs : EventArgs {
        public Board Board;

        public MyEventArgs(int playerId, Board board) {
            this.Board = board;
        }
    }
}