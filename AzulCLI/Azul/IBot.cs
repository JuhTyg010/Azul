using System.Collections.Generic;

namespace Azul {

    public interface IBot {
        public string WorkingDirectory { get; }
        public int Id { get; }
        
        public string DoMove(Board board);
        public string Place(Board board);
        public void Result(Dictionary<int, int> result);
    }
}