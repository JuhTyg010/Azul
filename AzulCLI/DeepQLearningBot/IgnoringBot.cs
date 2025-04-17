using System.Text.Json.Serialization;
using System.Transactions;
using Azul;
using SaveSystem;

namespace DeepQLearningBot;

public class IgnoringBot : IBot{
    
    public int Id { get; private set; }
    public string WorkingDirectory { get; private set; }
    
    private const string SettingFile = "IgnoringBot/DQNsettings.json";
    private const string ReplayBufferFile = "IgnoringBot/replayBuffer.json";
    private const string NetworkFile = "IgnoringBot/network.json";
    private DQNSetting _settings;
    private NeuralNetwork _policyNet;
    private NeuralNetwork _targetNet;
    private ReplayBuffer _replayBuffer;
    private Random _random;
    public bool SaveThis = false;
    public IgnoringBot(int id, string workingDirectory = null) {
        
        workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
        
        string networkDir = PathCombiner(WorkingDirectory, "IgnoringBot");
        if (!Path.Exists(networkDir)) Directory.CreateDirectory(networkDir);
        
        _settings = JsonSaver.Load<DQNSetting>(PathCombiner(WorkingDirectory,SettingFile)) ?? 
                                               new DQNSetting(300, 59, 300, 30, 1, 0.95, 0.01, 0.8);
        
        _policyNet = JsonSaver.Load<NeuralNetwork>(PathCombiner(WorkingDirectory, NetworkFile)) ?? 
                                                   new NeuralNetwork(_settings.StateSize, 128, 128, _settings.ActionSize);

        _targetNet = _policyNet.Clone();
        
        _replayBuffer = new ReplayBuffer(_settings.ReplayBufferCapacity);

        this.Id = id;
        _random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public string DoMove(Board board) {
        double[] state = board.EncodeBoardState(Id);

        int bestAction;
        if (_random.NextDouble() < _settings.Epsilon) {
            bestAction = GetRandomValidAction(board);
        }
        else {
            double[] qValues = _policyNet.Predict(state);
            bestAction = GetBestValidAction(qValues, board); // Greedy action
        }
        
        if(!IsLegalAction(bestAction, board)) throw new ApplicationException("Invalid action");
        
        double reward = CalculateReward(state, bestAction, board);
        
        Logger.WriteLine($"Move: {DecodeAction(bestAction)} reward: {reward}");
        var nextState = board.GetNextState(state, DecodeToMove(bestAction));

        _replayBuffer.Add(state, bestAction, reward, nextState, true);
        _settings.FromLastBatch++;
        if (_settings.FromLastBatch >= _settings.BatchSize) {
            _settings.FromLastBatch = 0;
            TrainFromReplayBuffer(board);
            _settings.Epsilon = Math.Max(_settings.EpsilonMin, _settings.Epsilon * _settings.EpsilonDecay);
        }

        return DecodeAction(bestAction);
    }

    public void Result(Dictionary<int, int> result) {
    }

    public void LoadNetwork(NeuralNetwork nc) {
        _policyNet = nc;
    }

    public NeuralNetwork GetNetwork() {
        return _policyNet;
    }

    private int GetRandomValidAction(Board board) {
        Move[] validMoves = board.GetValidMoves();
        if (validMoves.Length == 0)
            throw new Exception("No valid moves found!");
        int index = _random.Next(validMoves.Length);
        return EncodeAction(validMoves[index]);
    }

    public string Place(Board board) {

        return "-1";
    }
    
    private void TrainFromReplayBuffer(Board board) {
        // Sample a batch of experiences from the replay buffer
        var batch = _replayBuffer.Sample(_settings.BatchSize);
        var states = new double[_settings.BatchSize][];
        var targets = new double[_settings.BatchSize][];

        for (int i = 0; i < _settings.BatchSize; i++)
        {
            var replay = batch[i];
            double[] qValues = _policyNet.Predict(replay.State);

            double[] opponentQValues = _targetNet.Predict(replay.NextState);

            Move opponentPredicted = new Move(0,0,0);
            double maxVal = Max(opponentQValues);
            for (int index = 0; index < _settings.ActionSize; index++) {
                if (Math.Abs(opponentQValues[index] - maxVal) < 0.01) {
                    opponentPredicted = DecodeToMove(index);
                }
            }
            
            double[] ourNextState = board.GetNextState(replay.NextState, opponentPredicted);
            double[] ourNextQValues = _targetNet.Predict(ourNextState);

            // Update the Q-value for the taken action
            qValues[replay.Action] = replay.Reward + _settings.Gamma * Max(ourNextQValues);

            states[i] = replay.State;
            targets[i] = qValues;
        }

        // Train the neural network on the batch
        _policyNet.Train(states, targets, 0.001, 0.5);
        _targetNet = _policyNet.Clone();
    }

    private double CalculateReward(double[] state, int action, Board board) {
        double reward = 0;
        Move move = DecodeToMove(action);
        if (move.BufferId == Globals.WallDimension) return -10;
        var nextState = board.GetNextState(state, move);
        int takenCount = move.PlateId == board.Plates.Length 
            ? board.DecodePlateData((int) state[9])[move.TileId]
            : board.DecodePlateData((int) state[move.PlateId])[move.TileId];
        
        int col = board.FindColInRow(move.BufferId, move.TileId);
        var addedAfterFilled = board.Players[board.CurrentPlayer].AddedPointsAfterFilled(move.BufferId, col);
        
        
        
        var wall = board.Players[board.CurrentPlayer].wall;
        var inSameCol = Globals.WallDimension - Enumerable
            .Range(0, wall.GetLength(0))
            .Count(row => wall[row, col] == Globals.EmptyCell);
       
        var sameType = Enumerable.Range(0, wall.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                .Select(column => wall[row, column])).Count(value => value == move.TileId);
        
        var empty = Enumerable.Range(0, wall.GetLength(0))
            .SelectMany(row => Enumerable.Range(0, wall.GetLength(1))
                .Select(column => wall[row, column])).Count(value => value == Globals.EmptyCell);
        var filled = (Globals.WallDimension * Globals.WallDimension) - empty; 
        reward -= filled;

        reward += inSameCol;
        
        if (board.DecodeBufferData((int) nextState[11 + move.BufferId])[1] == move.BufferId + 1) {
            //reward += takenCount * .5;
            reward += 1 + .3 * takenCount;
            reward += 2 * addedAfterFilled;
            reward += sameType;
        }
        else {
            reward += 1;
            reward += addedAfterFilled * .5;
            //reward +=  takenCount;
        }

        var newOnFloor = nextState[16] - state[16];
        
        if(newOnFloor * 2 >= addedAfterFilled) reward -= newOnFloor;
        else reward += newOnFloor * 2;
        
        //TODO: maybe connect to addedAfterFilled
        //check if first from center
        if(Math.Abs(nextState[10] - state[10]) > .9) reward -= .1;
        
        return reward;
    }
    
    private int GetBestValidAction(double[] qValues, Board board) {
        int bestAction = -1;
        int incorrectCount = 0;
        double bestValue = double.MinValue;
        
        for (int action = 0; action < qValues.Length; action++) {
            if (IsLegalAction(action, board) && qValues[action] > bestValue) {
                bestValue = qValues[action];
                bestAction = action;
            } 
        }
        
        for (int action = 0; action < qValues.Length; action++) {
            if (qValues[action] > bestValue) {
                incorrectCount++;
                Logger.WriteLine($"tried invalid move: {DecodeAction(action)} qValues: {qValues[action]}");
                _replayBuffer.Add(board.EncodeBoardState(Id), action, -10, board.EncodeBoardState(Id), false);
            }
        }

        if (bestAction == -1) throw new IllegalOptionException("no valid action"); //should be always false
        Logger.WriteLine($"Best action's Qvalue: {bestValue} after {incorrectCount} failed actions");
        return bestAction;
    }

    private bool IsLegalAction(int action, Board board) {
        if (action < 0) return false;
        Move move = DecodeToMove(action);
        return board.CanMove(move);
    }
    
    private Move DecodeToMove(int actionId) {
        int tileId = actionId / (10 * 6);
        int plate = (actionId % (10 * 6)) / 6;
        int buffer = (actionId % 6);
        return new Move(tileId, plate, buffer);
    }

    private int EncodeAction(Move move) {
        int actionId = move.BufferId;
        actionId += 60 * move.TileId;
        actionId += 6 * move.PlateId;
        return actionId;
    }
    
    private string DecodeAction(int actionId) {
        Move move = DecodeToMove(actionId);
        return move.ToString();
    }
    
    private double Max(double[] values) {
        double max = double.MinValue;
        foreach (var value in values)
            if (value > max)
                max = value;
        return max;
    }
    
    private double Min(double[] values) {
        double min = double.MaxValue;
        foreach (var value in values)
            if (value < min)
                min = value;
        return min;
    }

    private void OnProcessExit(object sender, EventArgs e) {
        //JsonSaver.Save(settings, settingFile);
        //JsonSaver.Save(replayBuffer, replayBufferFile);
        JsonSaver.Save(_targetNet, PathCombiner(WorkingDirectory, NetworkFile));    
    }
    
    private static string PathCombiner(string baseName, string fileName) {
        if (baseName[^1] != '/') baseName += '/';
        return baseName + fileName;
    }
}

public class DQNSetting
{
    public int ActionSize { get; set; } //300
    public int StateSize { get; set; }
    public int ReplayBufferCapacity { get; set; }
    public int BatchSize { get; set; }
    
    public int FromLastBatch { get; set; }
    public double Epsilon { get; set; }
    public double EpsilonDecay { get; set; }
    public double EpsilonMin { get; set; }
    public double Gamma { get; set; }

    [JsonConstructor]
    public DQNSetting(int actionSize, int stateSize, int replayBufferCapacity, int batchSize, double epsilon, double epsilonDecay, double epsilonMin, double gamma)
    {
        ActionSize = actionSize;
        StateSize = stateSize;
        ReplayBufferCapacity = replayBufferCapacity;
        BatchSize = batchSize;
        Epsilon = epsilon;
        EpsilonDecay = epsilonDecay;
        EpsilonMin = epsilonMin;
        Gamma = gamma;
    }
}