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
        Move option = Globals.DefaultMove;
        int bestGain = Int32.MinValue;
        foreach (var possibleMove in possibleMoves) {
            int gain = GainIfPlayed(possibleMove, board);
            if (gain > bestGain) {
                option = possibleMove;
                bestGain = gain;
            }
        }
        return option.ToString();

    }

    public string Place(Azul.Board board) {
        Player me = board.Players[Id];
        var row = me.GetFullBuffersIds()[0];
        List<int> possiblePositions = new List<int>();
        for (int i = 0; i < Globals.WallDimension; i++) {
            if (PossibleCol(me.wall, row, i, me.GetBufferData(row).Id)) {
                possiblePositions.Add(i);
            }
        }

        return $"{possiblePositions[_random.Next(possiblePositions.Count)]}";
    }
    
    public void Result(Dictionary<int,int> result) {}

    private bool PossibleCol(int[,] wall, int row, int column, int chosenType) {
        for (int i = 0; i < Globals.WallDimension; i++) {
            if (wall[i, column] == chosenType) {
                return false;
            }
        }

        return wall[row, column] == Globals.EmptyCell;
    }
    
    private int GainIfPlayed(Move possibleMove, Azul.Board board) {
        int gain = 0;
        if (possibleMove.BufferId >= Globals.WallDimension) return -10;
        
        Player me = board.Players[Id];
        int bufferSize = possibleMove.BufferId + 1;
        Tile buffTile = me.GetBufferData(possibleMove.BufferId);
        Plate p = possibleMove.PlateId < board.Plates.Length ? board.Plates[possibleMove.PlateId] : board.Center;
        int toFill = p.TileCountOfType(possibleMove.TileId);
        if (buffTile.Id == possibleMove.TileId) {
            int toFloor = toFill - (bufferSize - buffTile.Count);
            if (toFloor >= 0) {
                gain -= toFloor;
                int clearGain = 0;
                if (board.IsAdvanced) {
                    int currGain = 0;
                    for (int col = 0; col < Globals.WallDimension; col++) {
                        currGain = me.AddedPointsAfterFilled(possibleMove.BufferId, col);
                        if(currGain > clearGain) clearGain = currGain;
                    }
                }
                else {
                    int row = possibleMove.BufferId;
                    int col = 0;
                    for(;col < Globals.WallDimension; col++)
                        if (board.PredefinedWall[row, col] == possibleMove.TileId)
                            break;
                    clearGain = me.AddedPointsAfterFilled(row,col);
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
                    for (int col = 0; col < Globals.WallDimension; col++) {
                        currGain = me.AddedPointsAfterFilled(possibleMove.BufferId, col);
                        if(currGain > clearGain) clearGain = currGain;
                    }
                }
                else {
                    int row = possibleMove.BufferId;
                    int col = 0;
                    for(;col < Globals.WallDimension; col++)
                        if (board.PredefinedWall[row, col] == possibleMove.TileId)
                            break;
                    clearGain = me.AddedPointsAfterFilled(row,col);
                }
                gain += clearGain;
            }
        }
        
        return gain;
        }
    }