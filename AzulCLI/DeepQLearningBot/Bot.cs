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
        this.id = id;
        
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

        int index = 51;
        (state, index) = AddPlayerData(index, state, board.Players[id]);

        foreach (Player p in board.Players) {
            if (p != board.Players[id]) {
                (state, index) = AddPlayerData(index, state, p);
            }
        }
        
        return state;
    }

    private (double[], int) AddPlayerData(int startIndex, double[] data, Player player) {
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
        return (data, startIndex);
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