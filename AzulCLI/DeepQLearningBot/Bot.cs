using System;
using System.Text.Json.Serialization;
using Azul;
using SaveSystem;

namespace DeepQLearningBot;

public class Bot : IBot {
    private const string settingFile = "/home/juhtyg/Desktop/Azul/AI_Data/ComplexBot/DQNsettings.json";
    private const string replayBufferFile = "/home/juhtyg/Desktop/Azul/AI_Data/ComplexBot/replayBuffer.json";
    private const string networkFile = "/home/juhtyg/Desktop/Azul/AI_Data/ComplexBot/network.json";
    private DQNSetting settings;
    private NeuralNetwork? policyNet;
    private NeuralNetwork targetNet;
    private ReplayBuffer replayBuffer;
    private Random random;
    private Dictionary<int, int> result;
    private int id;
    public Bot(int id) {
        /*DQNSetting setting = new DQNSetting(1,1,1,1,1,1,1,1);
        JsonSaver.Save(setting, settingFile);
        throw new NotImplementedException();*/
        
        Console.WriteLine("Constructor Called");
        settings = JsonSaver.Load<DQNSetting>(settingFile);
        Console.WriteLine("Constructor Called");
        policyNet = JsonSaver.Load<NeuralNetwork>(networkFile);
        if (policyNet == null) policyNet = new NeuralNetwork(settings.StateSize, 128, settings.ActionSize);

        targetNet = policyNet.Clone();

        replayBuffer = new ReplayBuffer(200);   //to ensure whole one game can fit in there

        this.id = id;
        random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public string DoMove(Board board)
    {
        double[] state = board.EncodeBoardState(settings.StateSize, id);

        int bestAction;
        if (random.NextDouble() < settings.Epsilon) {
            bestAction = GetRandomValidAction(board);
        }
        else {
            double[] qValues = policyNet.Predict(state);
            bestAction = GetBestValidAction(qValues, board); // Greedy action
        }
        
        if(!IsLegalAction(bestAction, board)) throw new ApplicationException("Invalid action");
        
        double reward = CalculateReward(state, bestAction, board);
        
        Logger.WriteLine("Move reward: " + reward);
        var nextState = board.GetNextState(state, DecodeToMove(bestAction), id);

        replayBuffer.Add(state, bestAction, reward, nextState, true);
        settings.FromLastBatch++;

        if (settings.FromLastBatch >= settings.BatchSize) {
            settings.FromLastBatch = 0;
            TrainFromReplayBuffer(replayBuffer);
            settings.Epsilon = Math.Max(settings.EpsilonMin, settings.Epsilon * settings.EpsilonDecay);
        }
        return DecodeAction(bestAction);
    }

    public int GetId() {
        return id;
    }

    private int GetRandomValidAction(Board board) {
        Move[] validMoves = board.GetValidMoves();
        if (validMoves.Length == 0)
            throw new Exception("No valid moves found!");
        int index = random.Next(validMoves.Length);
        return EncodeAction(validMoves[index]);
    }

    public string Place(Board board)
    {
        //TODO: setup for better translation
        double[] state = board.EncodeBoardState(settings.StateSize, id);

        // Get Q-values for placement
        double[] qValues = policyNet.Predict(state);
       // int bestPlacement = GetBestAction(qValues);

        return "-1";
    }
    
    private void TrainFromReplayBuffer(ReplayBuffer moveBuffer)
    {
        // Sample a batch of experiences from the replay buffer
        var batch = moveBuffer.Sample(settings.BatchSize);
        var states = new double[settings.BatchSize][];
        var targets = new double[settings.BatchSize][];

        for (int i = 0; i < settings.BatchSize; i++)
        {
            var replay = batch[i];

            // Predict Q-values for the current state
            double[] qValues = policyNet.Predict(replay.State);

            // Predict Q-values for the next state
            double[] nextQValues = targetNet.Predict(replay.NextState);

            // Update the Q-value for the taken action
            qValues[replay.Action] = replay.Reward + (replay.Reward > 0 ? settings.Gamma * Max(nextQValues) : 0);

            states[i] = replay.State;
            targets[i] = qValues;
        }

        // Train the neural network on the batch
        policyNet.Train(states, targets, 0.001);
        if (moveBuffer.Count % 100 == 0) {
            targetNet = policyNet.Clone();
        }
    }

    private double CalculateReward(double[] state, int action, Board board) {
        double reward = 0;
        Move move = DecodeToMove(action);
        if (move.bufferId == Globals.WALL_DIMENSION) return -1;
        
        double[] nextState = board.GetNextState(state, move, id);
        int col = board.FindColInRow(move.bufferId, move.tileId);
        
        reward += (double) board.Players[id].CalculatePointsIfFilled(move.bufferId, col) / 10;
        reward -= (nextState[56] - state[56]) / 10;    //floor
        //check if first from center
        if(Math.Abs(nextState[50] - state[50]) > .9) reward -= .5;
        
        return reward;
    }

    public void Result(Dictionary<int, int> result) {
        this.result = result;
    }

    
    private int GetBestValidAction(double[] qValues, Board board) {
        int bestAction = -1;
        double bestValue = double.MinValue;
        
        for (int action = 0; action < qValues.Length; action++) {
            if (IsLegalAction(action, board) && qValues[action] > bestValue) {
                bestValue = qValues[action];
                bestAction = action;
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

    private double ResultReward() {
        int max = int.MinValue;
        int key = 0;
        foreach (var value in result) {
            if (value.Value > max) {
                max = value.Value;
                key = value.Key;
            }
        }

        double reward;
        if (id == key) reward = 100;
        else reward = 100 * (result[id] - result[key]);
        return reward;
    }

    private void OnProcessExit(object sender, EventArgs e) {
        
        /*double reward = ResultReward();
        replayBuffer.UpdateRewards(reward * settings.Gamma);
        
        var storedReplays = JsonSaver.Load<ReplayBuffer>(replayBufferFile);
        if (storedReplays == null) storedReplays = replayBuffer;
        else {
            storedReplays.Marge(replayBuffer);
        }
        if (settings.FromLastBatch >= 5) {
            TrainFromReplayBuffer(storedReplays);
            settings.FromLastBatch = 0;
            settings.Epsilon = Math.Max(settings.EpsilonMin, settings.Epsilon * settings.EpsilonDecay);
        }
        else {
            settings.FromLastBatch++;
        }*/
        JsonSaver.Save(settings, settingFile);
        JsonSaver.Save(replayBuffer, replayBufferFile);
        JsonSaver.Save(targetNet, networkFile);
    }
}

public class DQNSetting
{
    public int ActionSize { get; set; } //300
    public int StateSize { get; set; } //ish 199
    public int ReplayBufferCapacity { get; set; }
    public int BatchSize { get; set; }
    
    public int FromLastBatch { get; set; }
    public double Epsilon { get; set; } // = 1.0;
    public double EpsilonDecay { get; set; } // = 0.995;
    public double EpsilonMin { get; set; } // = 0.01;
    public double Gamma { get; set; } // = 0.99;

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