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
    static List<double> imidiateRewards = new List<double>();
    
    public static void Run() {
        board.NextTakingMove += OnNextTakingTurn!;
        board.NextPlacingMove += OnNextPlacingTurn!;
        
        for (int episode = 0; episode < 1000; episode++) {
            Console.WriteLine($"Episode {episode}");
            board.StartGame();

            while (board.Phase != Phase.GameOver) { }
            
            ComputeFinalRewards();

            agent.Train(states, actions, rewards, probs);
            Console.WriteLine($"Episode {episode}: Reward = {rewards.Sum()}");
            
            states.Clear();
            actions.Clear();
            rewards.Clear();
            probs.Clear();
            
        }
        agent.SavePolicy(PolicyNetworkPath);
    }

    private static void OnNextPlacingTurn(object? sender, MyEventArgs e) {
        var game = e.board;
        var curr = game.CurrentPlayer;
        var player = game.Players[curr];
        Console.WriteLine($"Player {player.name} : placing");
        if (imidiateRewards.Count > 0) {
            AddFloorPenalty();
            rewards.AddRange(imidiateRewards);
            imidiateRewards.Clear();
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
        if (!board.CanMove(DecodeAction(action.Item1))) {
            //board.Move(Random.Shared.GetItems(game.GetValidMoves()))
            //todo: get random valid
        }
        states.Add(state);
        actions.Add(action.Item1);
        imidiateRewards.Add(CalculateReward(DecodeAction(action.Item1), state));
        probs.Add(action.Item2);
        //TODO: handle sizes of the numbers to prevent NaN
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
        //TODO: punish loosing sequence and reward winning one
        (int,int) added = (0,0);
        if (board.Players[0].pointCount > board.Players[1].pointCount) {
            added.Item1 = 10;
            added.Item2 = -10;
        }
        else {
            added.Item1 = -10;
            added.Item2 = 10;
        }

        for (int i = 0; i < rewards.Count; i++) {
            rewards[i] += i % 2 == 0 ? added.Item1 : added.Item2;
        }
    }

    private static void AddFloorPenalty() {
        //TODO: separate moves and states based on player and punish moves based on forcing to floor
        for (int i = 0; i < imidiateRewards.Count; i++) {
            imidiateRewards[i] -= board.Players[i % 2].floor.Count;           
        }
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