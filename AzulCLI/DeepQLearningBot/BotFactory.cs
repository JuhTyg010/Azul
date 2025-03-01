using Azul;

namespace DeepQLearningBot;

public class BotFactory {
    public static IBot CreateBot(string botType, int id) {
        return botType.ToLower() switch {
            "complex" => new Bot(id),
            "ignoring" => new IgnoringBot(id),
            _ => new Bot(id)//default is complex
        };
    }
}