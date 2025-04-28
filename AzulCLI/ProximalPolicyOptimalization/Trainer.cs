using System.Collections.Concurrent;
using Azul;
namespace PPO;

public class Trainer {
    public const int botsCount = 2;
    public const int sampleCount = 15;
    public const string LogPath = "/home/juhtyg/Desktop/Azul/proximalpolicyoptimalization.log";
    private const string ScorePath = "/home/juhtyg/Desktop/Azul/proximalscore.txt";
    public const string PolicyNetworkPath = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/policy_network.json";
    public const string ValueNetworkPath = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/value_network.json";
    static PPOAgent agent = new PPOAgent(stateSize: 199, actionSize: 300);
    Board board = new Board(botsCount,new string[]{"a","b"}, false, LogPath);
    static List<double[]> states = new List<double[]>();
    static List<int> actions = new List<int>();
    static List<double> rewards = new List<double>();
    static List<double[]> probs = new List<double[]>();
    static List<double>[] eachPlayerRewards = new List<double>[botsCount];
    private static bool wasReevaluated;
    
    private const int N = 20;  // parallel envs
    private const int M = 30; // steps per env per update
    private const long totalTimesteps = 1000_000;

    public static void Run() {
        EnvWrapper[] envs = new EnvWrapper[N];
        for (int i = 0; i < N; i++) envs[i] = new EnvWrapper(agent);

        long updates = totalTimesteps / (N * M);

        for (long update = 0; update < updates; update++) {
            ConcurrentBag<double[]> batchStates = new();
            ConcurrentBag<int> batchActions = new();
            ConcurrentBag<double> batchRewards = new();
            ConcurrentBag<double[]> batchProbs = new();

            for (int step = 0; step < M; step++) {
                Parallel.For(0, N, i => {
                    var (obs, reward, done, action, prob) = envs[i].Step();
                    if (done) envs[i].Reset();
                    if (action != -1) {
                        batchStates.Add(obs);
                        batchActions.Add(action);
                        batchRewards.Add(reward);
                        batchProbs.Add(prob);
                    }
                });
            }

            var statesList = batchStates.ToList();
            var actionsList = batchActions.ToList();
            var rewardsList = batchRewards.ToList();
            var probsList = batchProbs.ToList();

            agent.Train(statesList, actionsList, rewardsList, probsList);
            Console.WriteLine($"Update {update} | Avg Reward: {batchRewards.Average():F2}");
        }

        agent.SavePolicy(PolicyNetworkPath);
        agent.SaveValue(ValueNetworkPath);
    }

    private void OnNextPlacingTurn(object? sender, MyEventArgs e) {
        var game = e.Board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {player.name} : placing");
        if (!wasReevaluated) {
            AddFloorPenalty();
            wasReevaluated = true;
        }
        
        if (!game.IsAdvanced) {
            game.Calculate();
        }
        else {
            throw new NotImplementedException();
        }
    }

    private void OnNextTakingTurn(object? sender, MyEventArgs e) {
        var game = e.Board;
        double[] state = game.EncodeBoardState(game.CurrentPlayer);
        var validActions = game.GetValidMoves();
        
        //using multiple asking cause agent uses commutative counter
        var action = agent.SelectAction(state, validActions);
        int gain = GainIfPlayed(DecodeAction(action.Item1), board);
        for (int i = 0; i < sampleCount; i++) {
            var tempAction = agent.SelectAction(state, validActions);
            var tempGain = GainIfPlayed(DecodeAction(tempAction.Item1), board);
            if (tempGain > gain) {
                gain = tempGain;
                action = tempAction;
            }
        }
        
        states.Add(state);
        actions.Add(action.Item1);
        eachPlayerRewards[game.CurrentPlayer].Add(CalculateReward(DecodeAction(action.Item1), state, game.CurrentPlayer));
        wasReevaluated = false;
        probs.Add(action.Item2);
        if (!game.Move(DecodeAction(action.Item1))) {
            agent.SelectAction(state, validActions);
            throw new IllegalOptionException("Illegal move");
        }
    }

    public static Move DecodeAction(int action) {
        int tileId = action / (10 * 6);
        int plate = (action % (10 * 6)) / 6;
        int buffer = (action % 6);
        return new Move(tileId, plate, buffer);
    }

    private void AddFloorPenalty() {
        //TODO: separate moves and states based on player and punish moves based on forcing to floor
        for (int i = 0; i < botsCount; i++) {
            AddValueInRange(-board.Players[i].floor.Count * 4, ref eachPlayerRewards[i]);
        }
    }

    private static void AddValueInRange(double value, ref List<double> list) {
        for (int i = 0; i < list.Count; i++) {
            list[i] += value;
        }
    }

    public static double CalculateReward(Move move, double[] state, int playerId, int rewardFunId = 0) {
        switch (rewardFunId) {
            case 0: return RewardFunHowMuchTilesTook(move, state, playerId);
            case 1: return RewardFunWallFromLastMove(move, state, playerId);
            case 2: return RewardFunWallWithThisMove(move, state, playerId);
            case 3: return RewardFunLookIfNextOneHasBetterMove(move, state, playerId);
            default: return RewardFunHowMuchTilesTook(move, state, playerId);
        }
    }


    private static double RewardFunHowMuchTilesTook(Move move, double[] state, int playerId) {
        double reward = 0;
        
        if (move.BufferId == Globals.BufferCount) return -10;
        
        var nextState = Board.GetNextState(state, move);
        var oldMe = Board.DecodePlayer(Board.GetMyPlayerData(state));
        var newMe = Board.DecodePlayer(Board.GetMyPlayerData(nextState));
        int takenCount = move.PlateId == Board.PlateCount(state) 
            ? Board.DecodePlateData((int) state[9])[move.TileId]
            : Board.DecodePlateData((int) state[move.PlateId])[move.TileId];

        //filled buffer
        if (oldMe.GetFullBuffersIds().Length != newMe.GetFullBuffersIds().Length) {
            reward += 5;
            reward -= 2 * (newMe.floor.Count - oldMe.floor.Count);
        }
        else reward += takenCount;
        
        return reward;
    }

    private static double RewardFunWallFromLastMove(Move move, double[] state, int playerId) {
        double reward = 0;
        const double fullAdd = 1;
        const double fullBonus = 2;
        const double nonAdd = .5;
        const double nonBonus = .5;
        const double floorMultiplier = 2;
                              
        if (move.BufferId == Globals.BufferCount) return -10;
      
        var nextState = Board.GetNextState(state, move);
        
        var oldMe = Board.DecodePlayer(Board.GetMyPlayerData(state));
        var newMe = Board.DecodePlayer(Board.GetMyPlayerData(nextState));
        
        int col = Board.FindColInRow(move.BufferId, move.TileId);
        var addedPointsAfterFilled = newMe.AddedPointsAfterFilled(move.BufferId, col);
        
        var wall = newMe.wall;
        
        var empty = Enumerable.Range(0, wall.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                .Select(column => wall[row, column])).Count(value => value == Globals.EmptyCell);
        
        var filled = (Globals.WallDimension * Globals.WallDimension) - empty;
        reward -= filled; //to handle that added points are higher when more filled
        
        if (oldMe.GetFullBuffersIds().Length != newMe.GetFullBuffersIds().Length) {
            reward += fullAdd;
            reward += fullBonus * addedPointsAfterFilled;
            
            reward -= floorMultiplier * (newMe.floor.Count - oldMe.floor.Count);        }
        else {
            reward += nonAdd;
            reward += nonBonus * addedPointsAfterFilled;
        }

        return reward;
    }

    private static double RewardFunWallWithThisMove(Move move, double[] state, int playerId) {
        double reward = 0;
        const double fullAdd = 1;
        const double fullBonus = 2;
        const double nonAdd = .5;
        const double nonBonus = .5;
        const double floorMultiplier = 2;
                              
        if (move.BufferId == Globals.BufferCount) return -10;
        
        var nextState = Board.GetNextState(state, move);
        
        var oldMe = Board.DecodePlayer(Board.GetMyPlayerData(state));
        var newMe = Board.DecodePlayer(Board.GetMyPlayerData(nextState));
        //to check new move
        foreach (var row in oldMe.GetFullBuffersIds()) {
            var column = Board.FindColInRow(row, oldMe.GetBufferData(row).Id);
            Player.Fill(row, column, ref newMe);
        }
        var col = Board.FindColInRow(move.BufferId, move.TileId);
        var wall = newMe.wall;  
        
        var empty = Enumerable.Range(0, wall.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                .Select(column => wall[row, column])).Count(value => value == Globals.EmptyCell);
        
        var filled = (Globals.WallDimension * Globals.WallDimension) - empty;
        reward -= filled; //to handle that added points are higher when more filled
        
        var addedPointsAfterFilled = newMe.AddedPointsAfterFilled(move.BufferId, col);
        
        if (oldMe.GetFullBuffersIds().Length != newMe.GetFullBuffersIds().Length) {
            reward += fullAdd;
            reward += fullBonus * addedPointsAfterFilled;
            
            reward -= floorMultiplier * (newMe.floor.Count - oldMe.floor.Count);        }
        else {
            reward += nonAdd;
            reward += nonBonus * addedPointsAfterFilled;
        }
        
        return reward;
    }

    private static double RewardFunLookIfNextOneHasBetterMove(Move move, double[] state, int playerId) {
        double reward = 0;
        const double fullAdd = 1;
        const double fullBonus = 2;
        const double nonAdd = .5;
        const double nonBonus = .5;
        const double floorMultiplier = 2;
        
        if (move.BufferId == Globals.BufferCount) return -10;
        
        var nextState = Board.GetNextState(state, move);
        
        var oldMe = Board.DecodePlayer(Board.GetMyPlayerData(state));
        var newMe = Board.DecodePlayer(Board.GetMyPlayerData(nextState));
        
        //TODO: implement this
        
        return reward;
    }
    
    

    private static void ClearData() {
        for (int i = 0; i < botsCount; i++) {
            eachPlayerRewards[i].Clear();
        }
        states.Clear();
        actions.Clear();
        rewards.Clear();
        probs.Clear();
    }
    
    public static int EncodeMove(Move move) {
        int actionId = move.BufferId;
        actionId += 60 * move.TileId;
        actionId += 6 * move.PlateId;
        return actionId;
    }
    
    
    
    public static int GainIfPlayed(Move possibleMove, Azul.Board board) {
            int gain = 0;
            if (possibleMove.BufferId >= Globals.WallDimension) {
                return -10;
            }
            Player me = board.Players[board.CurrentPlayer];
            int bufferSize = possibleMove.BufferId + 1;
            Tile buffTile = me.GetBufferData(possibleMove.BufferId);
            Plate p = possibleMove.PlateId < board.Plates.Length ? board.Plates[possibleMove.PlateId] : board.Center;
            int toFill = p.TileCountOfType(possibleMove.TileId);
            if (buffTile.Id == possibleMove.TileId) {
                int toFloor = toFill - (bufferSize - buffTile.Count);
                if (toFloor >= 0) {
                    gain -= toFloor;
                    int clearGain = 0;
                    if (board.IsAdvanced) {
                        int currGain = 0;
                        for (int col = 0; col < Globals.WallDimension; col++) {
                            currGain = me.AddedPointsAfterFilled(possibleMove.BufferId, col);
                            if(currGain > clearGain) clearGain = currGain;
                        }
                    }
                    else {
                        int row = possibleMove.BufferId;
                        int col = 0;
                        for(;col < Globals.WallDimension; col++)
                            if (Board.PredefinedWall[row, col] == possibleMove.TileId)
                                break;
                        clearGain = me.AddedPointsAfterFilled(row,col);
                    }
                    gain += clearGain;
                }
            }
            else {
                int toFloor = bufferSize - toFill;
                if (toFloor >= 0) {
                    gain -= toFloor;
                    int clearGain = 0;
                    if (board.IsAdvanced) {
                        int currGain = 0;
                        for (int col = 0; col < Globals.WallDimension; col++) {
                            currGain = me.AddedPointsAfterFilled(possibleMove.BufferId, col);
                            if(currGain > clearGain) clearGain = currGain;
                        }
                    }
                    else {
                        int row = possibleMove.BufferId;
                        int col = 0;
                        for(;col < Globals.WallDimension; col++)
                            if (Board.PredefinedWall[row, col] == possibleMove.TileId)
                                break;
                        clearGain = me.AddedPointsAfterFilled(row,col);
                    }
                    gain += clearGain;
                }
            }
            
            return gain;
        }
}