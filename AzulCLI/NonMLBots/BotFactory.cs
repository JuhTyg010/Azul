using Azul;

namespace NonML;

public class BotFactory {

    private static readonly string[] Types = ["random", "heuristic"];
    public static IBot CreateBot(string botType, int id, string workingDir) {
        return botType.ToLower() switch {
            "random" => new RandomBot(id, workingDir),
            "heuristic" => new HeuristicBot(id, workingDir),
            _ => throw new IllegalOptionException($"Bot type \'{botType}\' wasn't recognised")
        };
    }

    public static bool WasRecognised(string botType) {
        return Types.Contains(botType.ToLower());
    }

}