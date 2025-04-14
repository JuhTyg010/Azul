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
    static Board board = new Board(botsCount,new string[]{"a","b"}, false, LogPath);
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

    private static void OnNextPlacingTurn(object? sender, MyEventArgs e) {
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

    private static void OnNextTakingTurn(object? sender, MyEventArgs e) {
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
        eachPlayerRewards[game.CurrentPlayer].Add(CalculateReward(DecodeAction(action.Item1), state, board));
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

    private static void AddFloorPenalty() {
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

    public static double CalculateReward(Move move, double[] state, Board board) {
        double reward = 0;
        
        if (move.bufferId == Globals.WALL_DIMENSION) return -10;
        var nextState = board.GetNextState(state, move);
        int takenCount = move.plateId == board.Plates.Length 
            ? board.DecodePlateData((int) state[9])[move.tileId]
            : board.DecodePlateData((int) state[move.plateId])[move.tileId];
        
        int col = board.FindColInRow(move.bufferId, move.tileId);
        var addedAfterFilled = board.Players[board.CurrentPlayer].CalculatePointsIfFilled(move.bufferId, col);
        var wall = board.Players[board.CurrentPlayer].wall;
        var inSameCol = Globals.WALL_DIMENSION - Enumerable
            .Range(0, wall.GetLength(0))
            .Count(row => wall[row, col] == Globals.EMPTY_CELL);
       
        var sameType = Enumerable.Range(0, wall.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                .Select(column => wall[row, column])).Count(value => value == move.tileId);
        
        var empty = Enumerable.Range(0, wall.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                .Select(column => wall[row, column])).Count(value => value == Globals.EMPTY_CELL);
        var filled = (Globals.WALL_DIMENSION * Globals.WALL_DIMENSION) - empty; 
        reward -= filled * .5;

        reward += sameType;
        if (board.DecodeBufferData((int) nextState[11 + move.bufferId])[1] == move.bufferId + 1) {
            //reward += takenCount * .5;
            reward += 3;
            reward += 2 * addedAfterFilled;
            reward += inSameCol;

        }
        else {
            reward += 1;
            reward += addedAfterFilled * .5;
            //reward +=  takenCount;
        }

        var newOnFloor = nextState[16] - state[16];
        
        if(newOnFloor * 2 >= addedAfterFilled) reward -= newOnFloor;
        else reward += newOnFloor * .2;
        
        //TODO: maybe connect to addedAfterFilled
        //check if first from center
        if(Math.Abs(nextState[10] - state[10]) > .9) reward -= .1;
        
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
        int actionId = move.bufferId;
        actionId += 60 * move.tileId;
        actionId += 6 * move.plateId;
        return actionId;
    }
    
    
    
    public static int GainIfPlayed(Move possibleMove, Azul.Board board) {
            int gain = 0;
            if (possibleMove.bufferId >= Globals.WALL_DIMENSION) {
                return -10;
            }
            Player me = board.Players[board.CurrentPlayer];
            int bufferSize = possibleMove.bufferId + 1;
            Tile buffTile = me.GetBufferData(possibleMove.bufferId);
            Plate p = possibleMove.plateId < board.Plates.Length ? board.Plates[possibleMove.plateId] : board.Center;
            int toFill = p.TileCountOfType(possibleMove.tileId);
            if (buffTile.id == possibleMove.tileId) {
                int toFloor = toFill - (bufferSize - buffTile.count);
                if (toFloor >= 0) {
                    gain -= toFloor;
                    int clearGain = 0;
                    if (board.IsAdvanced) {
                        int currGain = 0;
                        for (int col = 0; col < Globals.WALL_DIMENSION; col++) {
                            currGain = me.CalculatePointsIfFilled(possibleMove.bufferId, col);
                            if(currGain > clearGain) clearGain = currGain;
                        }
                    }
                    else {
                        int row = possibleMove.bufferId;
                        int col = 0;
                        for(;col < Globals.WALL_DIMENSION; col++)
                            if (board.PredefinedWall[row, col] == possibleMove.tileId)
                                break;
                        clearGain = me.CalculatePointsIfFilled(row,col);
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
                        for (int col = 0; col < Globals.WALL_DIMENSION; col++) {
                            currGain = me.CalculatePointsIfFilled(possibleMove.bufferId, col);
                            if(currGain > clearGain) clearGain = currGain;
                        }
                    }
                    else {
                        int row = possibleMove.bufferId;
                        int col = 0;
                        for(;col < Globals.WALL_DIMENSION; col++)
                            if (board.PredefinedWall[row, col] == possibleMove.tileId)
                                break;
                        clearGain = me.CalculatePointsIfFilled(row,col);
                    }
                    gain += clearGain;
                }
            }
            
            return gain;
        }
}