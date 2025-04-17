using System.Collections.Generic;

namespace Azul {

    public interface IBot {
        
        public string WorkingDirectory { get; }
        
        public int Id { get; }
        
        /// <summary>
        /// Method to decide what move the bot want's to play
        /// </summary>
        /// <param name="board"> Object where can be found all data of the current game</param>
        /// <returns>string in format {0-9} {0-5} {0-4}</returns>
        public string DoMove(Board board);
        
        
        /// <summary>
        /// Method which decides where on board bot wants to place tile
        /// </summary>
        /// <param name="board"> Object where can be found all data of the current game</param>
        /// <returns> string representing one integral value in rance 0-4</returns>
        public string Place(Board board);
        
        /// <summary>
        /// Method via which bots receives result of the game
        /// </summary>
        /// <param name="result"> dictionary where keys are ids of bots and values are points in the end of the game</param>
        public void Result(Dictionary<int, int> result);
    }
}