using System.Security.Cryptography;

namespace Azul;

public interface IBot {
    public string DoMove(Board board);
    public string Place(Board board);

    public int GetId();

    public void Result(Dictionary<int, int> result);
}