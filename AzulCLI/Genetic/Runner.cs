namespace Genetic;

public class Runner {
    
    public const int POPULATION_SIZE = 50;
    public const int GEN_COUNT = 100;
    public const int PLAYER_COUNT = 4;
    
    static void Main(string[] args) {
        Trainer trainer = new Trainer(POPULATION_SIZE);

        for (int i = 0; i < GEN_COUNT; i++) {
            trainer.RunGeneration(PLAYER_COUNT);
        }

    }
}