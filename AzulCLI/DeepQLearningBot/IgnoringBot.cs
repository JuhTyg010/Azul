using Azul;
using SaveSystem;
using System;

namespace DeepQLearningBot;

public class IgnoringBot : IBot{
    private const string settingFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/DQNsettings.json";
    private const string replayBufferFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/replayBuffer.json";
    private const string networkFile = "/home/juhtyg/Desktop/Azul/AI_Data/IgnoringBot/network.json";
    private DQNSetting settings;
    private NeuralNetwork? policyNet;
    private NeuralNetwork targetNet;
    private ReplayBuffer? replayBuffer;
    private Random random;
    private int id;
    public IgnoringBot(int id) {
        settings = JsonSaver.Load<DQNSetting>(settingFile);
        
        policyNet = JsonSaver.Load<NeuralNetwork>(networkFile);
        if (policyNet == null) policyNet = new NeuralNetwork(settings.StateSize, 128, settings.ActionSize);

        targetNet = policyNet.Clone();
        
        replayBuffer = new ReplayBuffer(settings.ReplayBufferCapacity);

        this.id = id;
        random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public string DoMove(Board board)
    {
        double[] state = board.EncodeBoardState(settings.StateSize, id, false);

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
        
        Logger.WriteLine($"Move: {DecodeAction(bestAction)} reward: {reward}");
        var nextState = board.GetNextState(state, DecodeToMove(bestAction), id);

        replayBuffer.Add(state, bestAction, reward, nextState, true);
        settings.FromLastBatch++;
        if (settings.FromLastBatch >= settings.BatchSize) {
            settings.FromLastBatch = 0;
            TrainFromReplayBuffer();
            settings.Epsilon = Math.Max(settings.EpsilonMin, settings.Epsilon * settings.EpsilonDecay);
        }

        return DecodeAction(bestAction);
    }

    public int GetId() {
        return id;
    }
    
    public void Result(Dictionary<int,int> result) {}

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
        double[] state = board.EncodeBoardState(settings.StateSize, id, false);

        // Get Q-values for placement
        double[] qValues = policyNet.Predict(state);
       // int bestPlacement = GetBestAction(qValues);

        return "-1";
    }
    
    private void TrainFromReplayBuffer()
    {
        // Sample a batch of experiences from the replay buffer
        var batch = replayBuffer.Sample(settings.BatchSize);
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
        //JsonSaver.Save(policyNet, networkFile);
        if (replayBuffer.Count % 100 == 0) {
            targetNet = policyNet.Clone();
        }
    }

    private double CalculateReward(double[] state, int action, Board board) {
        double reward = 0;
        
        Move move = DecodeToMove(action);
        if (move.bufferId == Globals.WALL_DIMENSION) return -10;
        
        double[] nextState = board.GetNextState(state, move, id);
        reward += 0.1 * board.Plates[move.plateId].TileCountOfType(move.tileId);
        int col = board.FindColInRow(move.bufferId, move.tileId);
        
        reward += (double) board.Players[id].CalculatePointsIfFilled(move.bufferId, col) / 10;
        reward -= (nextState[56] - state[56]) / 10;    //floor
        //check if first from center
        if(Math.Abs(nextState[50] - state[50]) > .9) reward -= .1;
        
        return reward;
    }
    
    private int GainIfPlayed(Move possibleMove, Azul.Board board) {
            int gain = 0;
            if (possibleMove.bufferId >= Globals.WALL_DIMENSION) {
                return -10;
            }
            Player me = board.Players[id];
            int bufferSize = possibleMove.bufferId + 1;
            Tile buffTile = me.GetBufferData(possibleMove.bufferId);
            Plate p = possibleMove.plateId < board.Plates.Length ? board.Plates[possibleMove.plateId] : board.Center;
            int toFill = p.TileCountOfType(possibleMove.tileId);
            if (buffTile.id == possibleMove.tileId) {
                int toFloor = toFill - (bufferSize - buffTile.count);
                if (toFloor >= 0) {
                    gain -= toFloor;
                    int clearGain = 0;
                    if (board.isAdvanced) {
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
                            if (board.predefinedWall[row, col] == possibleMove.tileId)
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
                    if (board.isAdvanced) {
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
                            if (board.predefinedWall[row, col] == possibleMove.tileId)
                                break;
                        clearGain = me.CalculatePointsIfFilled(row,col);
                    }
                    gain += clearGain;
                }
            }
            
            return gain;
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

    private void OnProcessExit(object sender, EventArgs e) {
        //JsonSaver.Save(settings, settingFile);
        //JsonSaver.Save(replayBuffer, replayBufferFile);
        JsonSaver.Save(targetNet, networkFile);    
    }
}