using Azul;

namespace Genetic;

public class Agent {
    public Dictionary<string, float> RuleWeights { get; set; }  // Rule name -> Weight
    public float Fitness { get; set; }                         // Performance metric

    public Agent(Dictionary<string, float> weights)
    {
        RuleWeights = weights;
        Fitness = 0;
    }

    // Evaluates a move based on weighted rules
    public float EvaluateMove(Board board, Move move)
    {
        float score = 0;
            
        if (Rules.CompletesRow(board, move))
            score += RuleWeights[nameof(Rules.CompletesRow)];
            
        if (Rules.BlocksOpponent(board, move))
            score += RuleWeights[nameof(Rules.BlocksOpponent)];
            
        // Add more rule checks...
            
        return score;
    }
}