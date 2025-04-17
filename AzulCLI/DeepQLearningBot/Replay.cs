namespace DeepQLearningBot;

public record struct Replay {
    public double[] State { get; set; }
    public int Action { get; set; }
    public double Reward { get; set; }
    public double[] NextState { get; set; }
    public bool Done { get; set; }

    public Replay(double[] state, int action, double reward, double[] nextState, bool done) {
        this.State = state;
        this.Action = action;
        this.Reward = reward;
        this.NextState = nextState;
        this.Done = done;
    }

    public void AddReward(double reward) {
        Reward += reward;
    }
}