namespace Genetic;

public class Runner {
    
    public const int POPULATION_SIZE = 64;
    public const int GEN_COUNT = 400;
    public const int PLAYER_COUNT = 2;
    public const double LIMIT = 2;
    public const double MARGIN = 4;
    
    static void Main(string[] args) {
        Trainer trainer = new Trainer(MARGIN, LIMIT);

        for (int i = 0; i < GEN_COUNT; i++) {
            trainer.RunGeneration(PLAYER_COUNT);
        }

    }
}