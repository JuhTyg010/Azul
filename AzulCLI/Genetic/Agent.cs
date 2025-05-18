using Azul;

namespace Genetic;

public class Agent {
    public Dictionary<string, double> RuleWeights { get; set; }  // Rule name -> Weight
    public int Played { get; set; }
    public int Wins { get; set; }

    public Agent(Dictionary<string, double> weights) {
        RuleWeights = weights;
        Played = 0;
        Wins = 0;
    }

    public double EvaluateMove(Board board, Move move) {
        double score = 0;

        var obj = new Rules();
        var methods = typeof(Rules).GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(Genetic.Rule), false).Any());

        foreach (var method in methods) {
            bool result = (bool) method.Invoke(null, new object[] { board, move })!;
            if (result) {
                string methodName = method.Name;
                if (RuleWeights.TryGetValue(methodName, out double weight)) {
                    score += weight;
                    //Console.WriteLine($"Rule {methodName} passed. +{weight} points.");
                }
            }
        }

        return score;
    }

    public void ResetValues() {
        Played = 0;
        Wins = 0;
    }

    public double WinRate() {
        return Wins / (double)Played;
    }

    public void PrintWeights() {
        foreach (var pair in RuleWeights) {
            Console.WriteLine($"{pair.Key}: {pair.Value}");
        }
    }
}