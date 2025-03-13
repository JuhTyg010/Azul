using Azul;
using SaveSystem;

namespace DeepQLearningBot;

public class IgnoringBot : IBot{
    private const string SettingFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/DQNsettings.json";
    private const string ReplayBufferFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/replayBuffer.json";
    private const string NetworkFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/network.json";
    private DQNSetting _settings;
    private NeuralNetwork _policyNet;
    private NeuralNetwork _targetNet;
    private ReplayBuffer _replayBuffer;
    private Random _random;
    private int _id;
    public bool SaveThis = false;
    public IgnoringBot(int id) {
        _settings = JsonSaver.Load<DQNSetting>(SettingFile) ?? 
                   new DQNSetting(300,199,300,30,1,0.95,0.01,0.8);
        
        _policyNet = JsonSaver.Load<NeuralNetwork>(NetworkFile) ?? 
                    new NeuralNetwork(_settings.StateSize, 128, _settings.ActionSize);

        _targetNet = _policyNet.Clone();
        
        _replayBuffer = new ReplayBuffer(_settings.ReplayBufferCapacity);

        this._id = id;
        _random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public string DoMove(Board board) {
        double[] state = board.EncodeBoardState(_id, false);

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
        var nextState = board.GetNextState(state, DecodeToMove(bestAction), _id);

        _replayBuffer.Add(state, bestAction, reward, nextState, true);
        _settings.FromLastBatch++;
        if (_settings.FromLastBatch >= _settings.BatchSize) {
            _settings.FromLastBatch = 0;
            TrainFromReplayBuffer(board);
            _settings.Epsilon = Math.Max(_settings.EpsilonMin, _settings.Epsilon * _settings.EpsilonDecay);
        }

        return DecodeAction(bestAction);
    }

    public int GetId() {
        return _id;
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

    public string Place(Board board)
    {
        //TODO: setup for better translation

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
            
            double[] ourNextState = board.GetNextState(replay.NextState, opponentPredicted, _id);
            double[] ourNextQValues = _targetNet.Predict(ourNextState);

            // Update the Q-value for the taken action
            qValues[replay.Action] = replay.Reward + _settings.Gamma * Max(ourNextQValues);

            states[i] = replay.State;
            targets[i] = qValues;
        }

        // Train the neural network on the batch
        _policyNet.Train(states, targets, 0.001);
        //JsonSaver.Save(policyNet, networkFile);
        if (_replayBuffer.Count % 100 == 0) {
            _targetNet = _policyNet.Clone();
        }
    }

    private double CalculateReward(double[] state, int action, Board board) {
        double reward = 0;
        
        Move move = DecodeToMove(action);
        if (move.bufferId == Globals.WALL_DIMENSION) return -1;
        
        double[] nextState = board.GetNextState(state, move, _id);
        if (move.plateId == board.Plates.Length) reward += 0.3 * board.Center.TileCountOfType(move.tileId);
        else reward += 0.3 * board.Plates[move.plateId].TileCountOfType(move.tileId);
        int col = board.FindColInRow(move.bufferId, move.tileId);
        
        reward += (double) board.Players[_id].CalculatePointsIfFilled(move.bufferId, col) / 10;
        reward -= (nextState[56] - state[56]) / 10;    //floor
        //check if first from center
        if(Math.Abs(nextState[50] - state[50]) > .9) reward -= .1;
        
        return reward;
    }
    
    private int GetBestValidAction(double[] qValues, Board board) {
        int bestAction = -1;
        double bestValue = double.MinValue;
        
        for (int action = 0; action < qValues.Length; action++) {
            if (IsLegalAction(action, board) && qValues[action] > bestValue) {
                bestValue = qValues[action];
                bestAction = action;
            } else if (qValues[action] > bestValue) {
                _replayBuffer.Add(board.EncodeBoardState(_id), action, -10, board.EncodeBoardState(_id), false);
            }
        }

        if (bestAction == -1) throw new IllegalOptionException("no valid action"); //should be always false
        Logger.WriteLine("Best action's Qvalue: " + bestValue);
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
        int actionId = move.bufferId;
        actionId += 60 * move.tileId;
        actionId += 6 * move.plateId;
        return actionId;
    }
    
    private string DecodeAction(int actionId) {
        Move move = DecodeToMove(actionId);
        return $"{move.plateId} {move.tileId} {move.bufferId}";
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
        if(SaveThis) JsonSaver.Save(_targetNet, NetworkFile);    
    }
}