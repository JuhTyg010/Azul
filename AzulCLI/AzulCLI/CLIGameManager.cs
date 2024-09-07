using Azul;

public class CLIGameManager {
    static void Main(string[] args) {
        Console.Write("Enter the number of players: ");
        //TODO: somehow determine humans and bots and their names
        /*try {*/
            int playerCount = int.Parse(Console.ReadLine());
            
            string[] names = new string[playerCount];
            for (int i = 0; i < playerCount; i++) {
                names[i] = $"Player {i}";
            }

            Board game = new Board(playerCount, names);

            while (game.Phase == Phase.Taking) {
                Console.WriteLine($" Next turn {names[game.CurrentPlayer]} (press enter to go)");
                Console.ReadLine();
                Writer.PrintBoard(game);
                string input = Console.ReadLine();
                int[] action = StringArrToIntArr(input.Split());
                // <{0-9}> <{0-4}> <{0-4}> first is which plate (last is center) second is type and last is buffer id
                bool moveDone = game.Move(action[0], action[1], action[2]);
                while (!moveDone) {
                    Console.WriteLine("Invalid move try again");
                    Writer.PrintBoard(game);
                    action = StringArrToIntArr(Console.ReadLine().Split());
                    moveDone = game.Move(action[0], action[1], action[2]);
                }
            }

            while (game.Phase == Phase.Placing) {
                Console.WriteLine($" Next turn {names[game.CurrentPlayer]} (press enter to go)");
                Console.ReadLine();
                Writer.PrintBoard(game);
                
            }
                
    }
            
            

        /*} catch (Exception e) {
            Console.WriteLine(e.Message);
            Environment.Exit(1);
        }*/
        
        private static int[] StringArrToIntArr(string[] arr) {
            List<int> output = new List<int>();

            foreach (var val in arr) {
                output.Add(int.Parse(val));
            }

            return output.ToArray();
        }
        
}
