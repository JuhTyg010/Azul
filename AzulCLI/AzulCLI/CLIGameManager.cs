namespace AzulCLI;

public class CLIGameManager {
    static void Main(string[] args) {
        Console.Write("Enter the number of players: ");
        try {
            int playerCount = int.Parse(Console.ReadLine());
            
            string[] names = new string[playerCount];
            for (int i = 0; i < playerCount; i++) {
                names[i] = $"Player {i}";
            }

            Board game = new Board(playerCount, names);

            

        } catch (Exception e) {
            Console.WriteLine(e.Message);
            Environment.Exit(1);
        }
        
        
        
    }
}