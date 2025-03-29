using Azul;
namespace PPO;

public class Trainer {
    private const int botsCount = 2;
    private const string LogPath = "/home/juhtyg/Desktop/Azul/proximalpolicyoptimalization.log";
    private const string PolicyNetworkPath = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/policy_network.json";
    private const string ValueNetworkPath = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/value_network.json";
    static PPOAgent agent = new PPOAgent(stateSize: 199, actionSize: 300);
    static Board board = new Board(botsCount,new string[]{"a","b"}, false, LogPath);
    static List<double[]> states = new List<double[]>();
    static List<int> actions = new List<int>();
    static List<double> rewards = new List<double>();
    static List<double[]> probs = new List<double[]>();
    static List<double>[] eachPlayerRewards = new List<double>[botsCount];
    static private bool wasReevaluated;
    
    public static void Run() {
        board.NextTakingMove += OnNextTakingTurn!;
        board.NextPlacingMove += OnNextPlacingTurn!;

        for (int i = 0; i < botsCount; i++) {
            eachPlayerRewards[i] = new List<double>();
        }
        
        for (int episode = 0; episode < 1000; episode++) {
            Console.WriteLine($"Episode {episode}");
            wasReevaluated = false;
            board.StartGame();

            while (board.Phase != Phase.GameOver) { }
            
            ComputeFinalRewards();

            agent.Train(states, actions, rewards, probs);
            Console.WriteLine($"Episode {episode}: Reward = {rewards.Sum()}");
            
            ClearData();
            
        }
        agent.SavePolicy(PolicyNetworkPath);
    }

    private static void OnNextPlacingTurn(object? sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {player.name} : placing");
        if (!wasReevaluated) {
            AddFloorPenalty();
            wasReevaluated = true;
        }
        
        if (!game.isAdvanced) {
            game.Calculate();
        }
        else {
            throw new NotImplementedException();
        }
    }

    private static void OnNextTakingTurn(object? sender, MyEventArgs e) {
        var game = e.board;
        double[] state = game.EncodeBoardState(game.CurrentPlayer);
        var validActions = game.GetValidMoves();
        
        
        var action = agent.SelectAction(state, validActions);
        states.Add(state);
        actions.Add(action.Item1);
        eachPlayerRewards[game.CurrentPlayer].Add(CalculateReward(DecodeAction(action.Item1), state));
        wasReevaluated = false;
        probs.Add(action.Item2);
        if (!game.Move(DecodeAction(action.Item1))) {
            agent.SelectAction(state, validActions);
            throw new IllegalOptionException("Illegal move");
        }
    }

    private static Move DecodeAction(int action) {
        int tileId = action / (10 * 6);
        int plate = (action % (10 * 6)) / 6;
        int buffer = (action % 6);
        return new Move(tileId, plate, buffer);
    }
    
    private static void ComputeFinalRewards() {
        int winnigPlayer = 0;
        int winnerCount = Int32.MinValue;
        for (int i = 0; i < botsCount; i++) {
            if (board.Players[i].pointCount > winnerCount) {
                winnigPlayer = i;
                winnerCount = board.Players[i].pointCount;
            }
        }

        for (int i = 0; i < botsCount; i++) {
            if (winnigPlayer == i) AddValueInRange(30, ref eachPlayerRewards[i]);
            else AddValueInRange(-30, ref eachPlayerRewards[i]);
        }

        for (int i = 0; i < botsCount; i++) {
            rewards.AddRange(eachPlayerRewards[i]);
        }

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


    private static double CalculateReward(Move move, double[] state) {
        double reward = 0;
        
        if (move.bufferId == Globals.WALL_DIMENSION) return -10;
        var nextState = board.GetNextState(state, move, board.CurrentPlayer);
        int takenCount = 0;
        if (move.plateId == board.Plates.Length) takenCount = board.Center.TileCountOfType(move.tileId);
        else takenCount = board.Plates[move.plateId].TileCountOfType(move.tileId);
        if (board.Players[board.CurrentPlayer].GetBufferData(move.bufferId).count + takenCount >= move.bufferId + 1) {
            reward += takenCount * 10;
        }
        else {
            return takenCount * 5;
        }
        int col = board.FindColInRow(move.bufferId, move.tileId);
        
        reward += 4 * board.Players[board.CurrentPlayer].CalculatePointsIfFilled(move.bufferId, col);
       // reward -= (nextState[56] - state[56]) * 2;    //floor
        //check if first from center
        if(Math.Abs(nextState[50] - state[50]) > .9) reward -= 2;
        
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
}