using Azul;

namespace NonML;

public class HeuristicBot : IBot {
    
    public int Id { get; private set; }
    public string WorkingDirectory { get; private set; }
    
    private readonly Random _random;

    public HeuristicBot(int id, string workingDirectory = null) {
        _random = new Random();
        this.Id = id;
        
        workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
    }

    public string DoMove(Azul.Board board) {

        var possibleMoves = board.GetValidMoves();
        Move option = new Move();
        int bestGain = Int32.MinValue;
        foreach (var possibleMove in possibleMoves) {
            int gain = GainIfPlayed(possibleMove, board);
            if (gain > bestGain) {
                option = possibleMove;
                bestGain = gain;
            }
        }
        return $"{option.plateId} {option.tileId} {option.bufferId}";

    }

    public string Place(Azul.Board board) {
        Player me = board.Players[Id];
        var row = me.FullBuffers()[0];
        List<int> possiblePositions = new List<int>();
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            if (PossibleCol(me.wall, row, i, me.GetBufferData(row).id)) {
                possiblePositions.Add(i);
            }
        }

        return $"{possiblePositions[_random.Next(possiblePositions.Count)]}";
    }
    
    public void Result(Dictionary<int,int> result) {}

    private bool PossibleCol(int[,] wall, int row, int column, int chosenType) {
        for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
            if (wall[i, column] == chosenType) {
                return false;
            }
        }

        return wall[row, column] == Globals.EMPTY_CELL;
    }
    
    private int GainIfPlayed(Move possibleMove, Azul.Board board) {
        int gain = 0;
        if (possibleMove.bufferId >= Globals.WALL_DIMENSION) return -10;
        
        Player me = board.Players[Id];
        int bufferSize = possibleMove.bufferId + 1;
        Tile buffTile = me.GetBufferData(possibleMove.bufferId);
        Plate p = possibleMove.plateId < board.Plates.Length ? board.Plates[possibleMove.plateId] : board.Center;
        int toFill = p.TileCountOfType(possibleMove.tileId);
        if (buffTile.id == possibleMove.tileId) {
            int toFloor = toFill - (bufferSize - buffTile.count);
            if (toFloor >= 0) {
                gain -= toFloor;
                int clearGain = 0;
                if (board.IsAdvanced) {
                    int currGain = 0;
                    for (int col = 0; col < Globals.WALL_DIMENSION; col++) {
                        currGain = me.CalculatePointsIfFilled(possibleMove.bufferId, col);
                        if(currGain > clearGain) clearGain = currGain;
                    }
                }
                else {
                    int row = possibleMove.bufferId;
                    int col = 0;
                    for(;col < Globals.WALL_DIMENSION; col++)
                        if (board.PredefinedWall[row, col] == possibleMove.tileId)
                            break;
                    clearGain = me.CalculatePointsIfFilled(row,col);
                }
                gain += clearGain;
            }
        }
        else {
            int toFloor = bufferSize - toFill;
            if (toFloor >= 0) {
                gain -= toFloor;
                int clearGain = 0;
                if (board.IsAdvanced) {
                    int currGain = 0;
                    for (int col = 0; col < Globals.WALL_DIMENSION; col++) {
                        currGain = me.CalculatePointsIfFilled(possibleMove.bufferId, col);
                        if(currGain > clearGain) clearGain = currGain;
                    }
                }
                else {
                    int row = possibleMove.bufferId;
                    int col = 0;
                    for(;col < Globals.WALL_DIMENSION; col++)
                        if (board.PredefinedWall[row, col] == possibleMove.tileId)
                            break;
                    clearGain = me.CalculatePointsIfFilled(row,col);
                }
                gain += clearGain;
            }
        }
        
        return gain;
        }
    }