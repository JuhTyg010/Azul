using Azul;

namespace NonML;

public class MinmaxBot : IBot {
    public string WorkingDirectory { get; private set; }
    public int Id { get; private set; }

    public MinmaxBot(int id, string workingDirectory = null) {
        this.Id = id;
        workingDirectory ??= Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
    }
    public string DoMove(Board board) {
        throw new NotImplementedException();
    }

    public string Place(Board board) {
        throw new NotImplementedException();
    }

    public void Result(Dictionary<int, int> result) { }

    private int CountValue(Board board, Move move, int playerId, int depth) {
        Board board2 = board;
        board2.Move(move);
        if (board2.Phase == Phase.Placing || depth == 0) {
            //TODO: math
            while (board2.Phase == Phase.Placing) {
                board2.Calculate();
            }

            return 0;
        }
        int value = 0;
        foreach (var nextMove in board2.GetValidMoves()) {
            int possibleValue = CountValue(board2, nextMove, playerId, depth - 1);
            if (possibleValue > value) {
                value = possibleValue;
            }
        }
        return value;
    }
}