using Azul;

namespace Genetic;

public class Bot : IBot
{
    public string WorkingDirectory { get; }
    public int Id { get; }
    private Agent _agent;

    public Bot(int id, Agent agent, string workingDirectory) {
        Id = id;
        _agent = agent;
        WorkingDirectory = workingDirectory;
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
        int bestScore = int.MinValue;
        int winner = 0;
        foreach (var pair in result) {
            if (pair.Value > bestScore) {
                bestScore = pair.Value;
                winner = pair.Key;
            }
        }

        if (winner == Id) {
            _agent.Fitness += 1;
            Console.WriteLine($"Id: {Id} Fitness: {_agent.Fitness}");
        }
    }
    
    public Agent GetAgent => _agent;
}