using Azul;
using SaveSystem;

namespace DeepQLearningBot;

public class Bot {
    private const string settingFile = "DQNsettings.json";
    private DQNTrainer trainer;
    private int stateSize; //ish 199
    private int actionSize; //300
    public int id { get; private set; }


    public Bot(int id) {
        DQNSetting setting = JsonSaver.Load<DQNSetting>(settingFile);
        trainer = new DQNTrainer(setting);
        this.stateSize = setting.stateSize;
        this.actionSize = setting.actionSize;
        
    }

    public string DoMove(Board board)
    {
        double[] state = EncodeBoardState(board);

        double[] qValues = trainer.Predict(state);
        int bestAction = GetBestAction(qValues);

        // Convert best action index to move string
        return DecodeAction(bestAction, board);
    }

    public string Place(Board board)
    {
        //TODO: setup for better translation
        double[] state = EncodeBoardState(board);

        // Get Q-values for placement
        double[] qValues = trainer.Predict(state);
        int bestPlacement = GetBestAction(qValues);

        return bestPlacement.ToString();
    }
    
    private double[] EncodeBoardState(Board board)
    {
        double[] state = new double[stateSize];

        // TODO: Implement encoding logic based on board state
        //for plates takes max 45
        for (int i = 0; i < board.Plates.Length; i++) {
            for (int j = 0; j < Globals.TYPE_COUNT; j++) {
                state[(i * Globals.TYPE_COUNT) + j] = board.Plates[i].TileCountOfType(j);
            }
        }
        // center plate
        for (int i = 0; i < Globals.TYPE_COUNT; i++) {
            state[45 + i] = board.Center.TileCountOfType(i);
        }
        state[50] = board.Center.isFirst ? 0 : 1;
        
        //own data
        int myIndex = board.CurrentPlayer;
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            var buffer = board.Players[myIndex].GetBufferData(i);
            state[51 + (2 * i)] = buffer.id;
            state[52 + (2 * i)] = buffer.count;
        }
        //own floor
        state[61] = board.Players[myIndex].floor.Count;
        state[62] = board.Players[myIndex].isFirst ? 0 : 1;
        //own wall
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                state[63 + (i * Globals.WALL_DIMENSION) + j] = board.Players[myIndex].wall[i, j];
            }
        }
        
        //TODO: other players data 
        
        
        
        return state;
    }

    private int GetBestAction(double[] qValues)
    {
        int bestAction = 0;
        double maxValue = double.MinValue;

        for (int i = 0; i < qValues.Length; i++) {
            if (qValues[i] > maxValue) {
                maxValue = qValues[i];
                bestAction = i;
            }
        }

        return bestAction;
    }
    
    private string DecodeAction(int actionIndex, Board board)
    {
        // TODO: Implement conversion logic
        int tileId = actionIndex / (10 * 6);
        int plate = (actionIndex % (10 * 6)) / 6;
        int buffer = (actionIndex % 6);
        return $"{plate} {tileId} {buffer}";
    }
}

public struct DQNSetting {
    public int actionSize;
    public int stateSize;
    public int replayBufferCapacity;

    public DQNSetting(int actionSize, int stateSize, int replayBufferCapacity) {
        this.actionSize = actionSize;
        this.stateSize = stateSize;
        this.replayBufferCapacity = replayBufferCapacity;
    }
}