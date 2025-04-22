using Azul;

namespace DeepQLearningBot;

public class BotFactory {
    
    
    private static readonly string[] Types = ["ignoring"];
    public static IBot CreateBot(string botType, int id) {
        return botType.ToLower() switch {
            "ignoring" => new IgnoringBot(id),
            _ => throw new IllegalOptionException($"Bot type \'{botType}\' wasn't recognised")
        };
    }
}