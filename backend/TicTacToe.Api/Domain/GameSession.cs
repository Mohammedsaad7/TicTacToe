namespace TicTacToe.Api.Domain;

public enum GameMode { TwoPlayer, Computer }
public enum GameStatus { InProgress, Won, Draw }
public sealed record Move(int Number, char Player, int Row, int Column);

/// <summary>
/// Mutated only while GameStore's lock is held. All rules live in this domain type.
/// </summary>
public sealed class GameSession
{
    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    public Guid Id { get; } = Guid.NewGuid();
    public GameMode Mode { get; }
    public char?[] Board { get; } = new char?[9];
    public List<Move> History { get; } = [];
    public char CurrentPlayer { get; private set; } = 'X';
    public GameStatus Status { get; private set; } = GameStatus.InProgress;
    public char? Winner { get; private set; }
    public int[] WinningCells { get; private set; } = [];

    public GameSession(GameMode mode) => Mode = mode;

    public void Play(char player, int row, int column)
    {
        if (Status != GameStatus.InProgress)
            throw new GameRuleException("This game is already complete.");
        if (row is < 0 or > 2 || column is < 0 or > 2)
            throw new GameRuleException("Row and column must each be 0, 1, or 2.");
        if (player != CurrentPlayer)
            throw new GameRuleException($"It is {CurrentPlayer}'s turn.");
        if (Mode == GameMode.Computer && player == 'O')
            throw new GameRuleException("Computer moves cannot be submitted by the client.");

        AddMove(player, row, column);
        if (Mode == GameMode.Computer && Status == GameStatus.InProgress)
        {
            var index = ComputerStrategy.ChooseMove(Board);
            // Only the server chooses and applies the computer's move.
            AddMove('O', index / 3, index % 3);
        }
    }

    private void AddMove(char player, int row, int column)
    {
        var index = row * 3 + column;
        if (Board[index].HasValue)
            throw new GameRuleException("That cell is already occupied.");

        Board[index] = player;
        History.Add(new Move(History.Count + 1, player, row, column));
        Recalculate();
        if (Status == GameStatus.InProgress)
            CurrentPlayer = player == 'X' ? 'O' : 'X';
    }

    public void Undo()
    {
        if (Status != GameStatus.InProgress)
            throw new GameRuleException("Undo is disabled after a game completes.");
        if (History.Count == 0)
            throw new GameRuleException("There are no moves to undo.");

        // Computer actions are atomic with the preceding human move.
        var count = Mode == GameMode.Computer && History[^1].Player == 'O' ? 2 : 1;
        for (var i = 0; i < count; i++)
        {
            var move = History[^1];
            Board[move.Row * 3 + move.Column] = null;
            History.RemoveAt(History.Count - 1);
        }

        Recalculate();
        CurrentPlayer = History.Count == 0 ? 'X'
            : History[^1].Player == 'X' ? 'O' : 'X';
    }

    public void Reset()
    {
        Array.Clear(Board);
        History.Clear();
        CurrentPlayer = 'X';
        Recalculate();
    }

    private void Recalculate()
    {
        Winner = null;
        WinningCells = [];
        Status = GameStatus.InProgress;
        foreach (var line in Lines)
        {
            var mark = Board[line[0]];
            if (mark.HasValue && Board[line[1]] == mark && Board[line[2]] == mark)
            {
                Winner = mark;
                WinningCells = [.. line];
                Status = GameStatus.Won;
                return;
            }
        }
        if (Board.All(cell => cell.HasValue))
            Status = GameStatus.Draw;
    }
}

public sealed class GameRuleException(string message) : Exception(message) { }
