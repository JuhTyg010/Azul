using Azul;
using randomBot;

public class CLIGameManager {
    static void Main(string[] args) {
        Console.Write("Enter game mode (advanced 1/basic 0): ");
        string mode = Console.ReadLine();
        bool isAdvanced = mode == "advanced" || mode == "1";
        Console.Write("Enter the players names (H/B_name: ");
        //TODO: somehow determine humans and bots and their names
        /*try {*/
            string[] playerSetup = Console.ReadLine().Split();
            
            List<randomBot.Bot> botPlayers = new List<randomBot.Bot>();
            for (int i = 0; i < playerSetup.Length; i++) {
                if (playerSetup[i].Split("_")[0] == "B") {
                    botPlayers.Add(new Bot(i));
                }
            }
            int[] botIds = new int[botPlayers.Count];
            
            for (int i = 0; i < botPlayers.Count; i++){
                botIds[i] = botPlayers[i].id;
            }
            
            string[] names = new string[playerSetup.Length];
            for (int i = 0; i < playerSetup.Length; i++) {
                names[i] = playerSetup[i];
            }

            Board game = new Board(playerSetup.Length, names, isAdvanced);

            while (game.Phase != Phase.GameOver) {
                while (game.Phase == Phase.Taking) {
                    Console.WriteLine($" Next turn {names[game.CurrentPlayer]} (press enter to go)");
                    if (botIds.Contains(game.CurrentPlayer)) {
                        Writer.PrintBoard(game);
                        string botMove = botPlayers[findBot(game.CurrentPlayer, botPlayers)].DoMove(game);
                        Console.WriteLine(botMove);
                        int[] action = StringArrToIntArr(botMove.Split(' '));
                        game.Move(action[0], action[1], action[2]); //bot cant do illegal move (for now)
                    }
                    else {
                        Console.ReadLine();
                        Writer.PrintBoard(game);

                        string input = Console.ReadLine();
                        int[] action = StringArrToIntArr(input.Split());
                        // <{0-9}> <{0-4}> <{0-5}> first is which plate (last is center) second is type and last is buffer id
                        bool moveDone = game.Move(action[0], action[1], action[2]);
                        while (!moveDone) {
                            Console.WriteLine("Invalid move try again");
                            Writer.PrintBoard(game);
                            action = StringArrToIntArr(Console.ReadLine().Split());
                            moveDone = game.Move(action[0], action[1], action[2]);
                        }
                    }
                }

                while (game.Phase == Phase.Placing) {
                    Console.WriteLine($" Next turn in filling {names[game.calculating.x]} (press enter to go)");
                    if(!botIds.Contains(game.calculating.x)) Console.ReadLine();
                    if (!game.isAdvanced) {
                        int currentPlayer = game.calculating.x;
                        while (currentPlayer == game.calculating.x) {
                            game.Calculate();
                        }

                        Writer.PrintBoard(game);
                    }
                    else {
                        Writer.PrintBoard(game);
                        int currentPlayer = game.calculating.x;
                        while (currentPlayer == game.calculating.x) {
                            if (botIds.Contains(game.calculating.x)) {
                                game.Calculate(int.Parse(botPlayers[findBot(currentPlayer, botPlayers)].Place(game)));
                            }
                            else {
                                string input = Console.ReadLine(); //should be {0-4} representing the column of first buffer
                                game.Calculate(int.Parse(input));
                            }

                            Writer.PrintBoard(game);
                        }
                    }
                }
            }

            Console.WriteLine("Game over");
            //TODO: write scores and stuff
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

        private static int findBot(int botId, List<randomBot.Bot> botPlayers) {
            for (int i = 0; i < botPlayers.Count; i++) {
                if (botPlayers[i].id == botId) {
                    return i;
                }
            }

            return -1;
        }

}
