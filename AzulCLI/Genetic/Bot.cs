using Azul;

namespace Genetic;

public class Bot : IBot
{
    public string WorkingDirectory { get; }
    public int Id { get; }
    private readonly Agent _agent;

    public Bot(int id, Agent agent) {
        Id = id;
        _agent = agent;
    }

    public string DoMove(Board board) {
        var possibleMoves = board.GetValidMoves();
        var bestMove = possibleMoves.OrderByDescending(m => _agent.EvaluateMove(board, m)).First();
        return bestMove.ToString();
    }

    public string Place(Board board) {
        return "0";//TODO for now just dummy
    }

    public void Result(Dictionary<int, int> result) {
        return;
    }
}