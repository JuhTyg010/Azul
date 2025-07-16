using Azul;

namespace DeepQLearningBot;

public class BotFactory {
    
    
    private static readonly string[] Types = ["DQN"];
    public static IBot CreateBot(string botType, int id, int rewardType) {
        return botType.ToLower() switch {
            "DQN" => new IgnoringBot(id, rewardType),
            _ => throw new IllegalOptionException($"Bot type \'{botType}\' wasn't recognised")
        };
    }
}