namespace TicTacToe.Api.Domain;

/// <summary>Deterministic priority: win > block > center > corner > any empty square.</summary>
public static class ComputerStrategy
{
    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    public static int ChooseMove(IReadOnlyList<char?> board)
    {
        if (board.Count != 9)
            throw new ArgumentException("Board must contain exactly nine cells.", nameof(board));

        var winning = FindWinningMove(board, 'O');
        if (winning.HasValue) return winning.Value;
        var blocking = FindWinningMove(board, 'X');
        if (blocking.HasValue) return blocking.Value;
        if (!board[4].HasValue) return 4;
        foreach (var corner in new[] { 0, 2, 6, 8 })
            if (!board[corner].HasValue) return corner;
        for (var i = 0; i < 9; i++)
            if (!board[i].HasValue) return i;

        throw new InvalidOperationException("There are no valid computer moves.");
    }

    private static int? FindWinningMove(IReadOnlyList<char?> board, char mark)
    {
        foreach (var line in Lines)
        {
            var marked = line.Count(i => board[i] == mark);
            if (marked == 2)
            {
                var empty = line.Where(i => !board[i].HasValue).ToArray();
                if (empty.Length == 1) return empty[0];
            }
        }
        return null;
    }
}
