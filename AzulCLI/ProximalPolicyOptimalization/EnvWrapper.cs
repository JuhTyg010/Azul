using Azul;
namespace PPO;

public class EnvWrapper {
    private Board board;
    private PPOAgent agent;
    private bool done = false;
    private double[] currentObs;

    private List<double[]> states = new();
    private List<int> actions = new();
    private List<double> rewards = new();
    private List<double[]> probs = new();

    public EnvWrapper(PPOAgent agent) {
        this.agent = agent;
        board = new Board(Trainer.botsCount, new string[] { "a", "b" }, false, Trainer.LogPath);
        board.NextTakingMove += OnNextTakingTurn!;
        board.NextPlacingMove += OnNextPlacingTurn!;
        Reset();
    }

    public void Reset() {
        board.StartGame();
        done = false;
        states.Clear();
        actions.Clear();
        rewards.Clear();
        probs.Clear();
        currentObs = board.EncodeBoardState(board.CurrentPlayer);
    }

    public (double[] obs, double reward, bool done, int action, double[] prob) Step() {

        while (board.Phase == Phase.Placing) {
            board.Calculate();
        }
        
        if (done || board.Phase == Phase.GameOver) {
            Console.WriteLine("Game Over reached");
            done = true;
            return (currentObs, 0, true, -1, Array.Empty<double>());
        }

        var validMoves = board.GetValidMoves();
        int actionId;
        double[] actionProbs;
        lock (agent) {
            (actionId, actionProbs) = agent.SelectAction(currentObs, validMoves);
        }
        var move = Trainer.DecodeAction(actionId);
        double reward = Trainer.CalculateReward(move, currentObs, board.CurrentPlayer);
        bool legal = board.Move(move);
        if (!legal) {
            reward -= 5; // penalize illegal
            done = true;
        }

        states.Add(currentObs);
        actions.Add(actionId);
        rewards.Add(reward);
        probs.Add(actionProbs);

        if (board.Phase == Phase.GameOver) {
            done = true;
        }
        else {
            currentObs = board.EncodeBoardState(board.CurrentPlayer);
        }

        return (currentObs, reward, done, actionId, actionProbs);
    }

    public (List<double[]>, List<int>, List<double>, List<double[]>) GetTrajectoryData() {
        return (states, actions, rewards, probs);
    }

    private void OnNextTakingTurn(object? sender, MyEventArgs e) {
    }

    private void OnNextPlacingTurn(object? sender, MyEventArgs e) {
        if (!board.IsAdvanced) {
            board.Calculate();
        }
    }
}
