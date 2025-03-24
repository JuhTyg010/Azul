using Azul;
namespace PPO;

public class Trainer {
    private const string LogPath = "/home/juhtyg/Desktop/Azul/proximalpolicyoptimalization.log";
    private const string PolicyNetworkPath = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/policy_network.json";
    private const string ValueNetworkPath = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/value_network.json";
    static PPOAgent agent = new PPOAgent(stateSize: 199, actionSize: 300);
    static Board board = new Board(2,new string[]{"a","b"}, false, LogPath);
    static List<double[]> states = new List<double[]>();
    static List<int> actions = new List<int>();
    static List<double> rewards = new List<double>();
    static List<double[]> probs = new List<double[]>();
    
    public static void Run() {
        board.NextTakingMove += OnNextTakingTurn!;
        board.NextPlacingMove += OnNextPlacingTurn!;
        
        for (int episode = 0; episode < 1000; episode++) {
            Console.WriteLine($"Episode {episode}");
            board.StartGame();

            while (board.Phase != Phase.GameOver) { }

            agent.Train(states, actions, rewards, probs);
            Console.WriteLine($"Episode {episode}: Reward = {rewards.Sum()}");
        }
        agent.SavePolicy(PolicyNetworkPath);
    }

    private static void OnNextPlacingTurn(object? sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {player.name} : placing");
        
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
        rewards.Add(CalculateReward(DecodeAction(action.Item1), state));
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

    private static double CalculateReward(Move move, double[] state) {
        double reward = 0;
        
        if (move.bufferId == Globals.WALL_DIMENSION) return -1;
        var nextState = board.GetNextState(state, move, board.CurrentPlayer);
        if (move.plateId == board.Plates.Length) reward += 0.3 * board.Center.TileCountOfType(move.tileId);
        else reward += 0.3 * board.Plates[move.plateId].TileCountOfType(move.tileId);
        int col = board.FindColInRow(move.bufferId, move.tileId);
        
        reward += (double) board.Players[board.CurrentPlayer].CalculatePointsIfFilled(move.bufferId, col) / 10;
        reward -= (nextState[56] - state[56]) / 10;    //floor
        //check if first from center
        if(Math.Abs(nextState[50] - state[50]) > .9) reward -= .1;
        
        return reward;
    }
}