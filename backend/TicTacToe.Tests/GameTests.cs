using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;
using Xunit;

namespace TicTacToe.Tests;

public sealed class GameTests
{
    private static (GameStore Store, Guid Id) NewGame(string mode = "TwoPlayer")
    {
        var store = new GameStore();
        return (store, store.Create(mode).GameId);
    }

    private static void RowWin(GameStore store, Guid id)
    {
        store.Play(id, "X", 0, 0);
        store.Play(id, "O", 1, 0);
        store.Play(id, "X", 0, 1);
        store.Play(id, "O", 1, 1);
        store.Play(id, "X", 0, 2);
    }

    [Fact]
    public void ValidMove_StoresMarkHistoryAndSwitchesTurn()
    {
        var (store, id) = NewGame();
        var game = store.Play(id, "X", 1, 2);
        Assert.Equal("X", game.Board[5]);
        Assert.Equal("O", game.CurrentPlayer);
        Assert.Equal("InProgress", game.Status);
        Assert.Single(game.MoveHistory);
        Assert.Equal((1, "X", 1, 2), (game.MoveHistory[0].Number, game.MoveHistory[0].Player,
            game.MoveHistory[0].Row, game.MoveHistory[0].Column));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 2)]
    [InlineData(0, 3)]
    public void OutsideBoardRejected_WithoutChangingState(int row, int column)
    {
        var (store, id) = NewGame();
        Assert.Throws<GameRuleException>(() => store.Play(id, "X", row, column));
        Assert.Empty(store.Get(id).MoveHistory);
        Assert.Equal("X", store.Get(id).CurrentPlayer);
    }

    [Fact]
    public void OccupiedCellRejected_WithoutChangingTurn()
    {
        var (store, id) = NewGame();
        store.Play(id, "X", 0, 0);
        Assert.Throws<GameRuleException>(() => store.Play(id, "O", 0, 0));
        Assert.Equal("O", store.Get(id).CurrentPlayer);
        Assert.Single(store.Get(id).MoveHistory);
    }

    [Fact]
    public void WrongPlayerRejected()
    {
        var (store, id) = NewGame();
        Assert.Throws<GameRuleException>(() => store.Play(id, "O", 0, 0));
        Assert.Empty(store.Get(id).MoveHistory);
    }

    [Fact]
    public void RowWin_HighlightsThreeCellsAndCreditsXOnce()
    {
        var (store, id) = NewGame();
        RowWin(store, id);
        var game = store.Get(id);
        Assert.Equal("Won", game.Status);
        Assert.Equal("X", game.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, game.WinningCells);
        Assert.Equal(1, store.GetScoreboard().XWins);
        Assert.False(game.CanUndo);
        Assert.Equal(1, store.Get(id).Scoreboard.XWins);
    }

    [Fact]
    public void ColumnWin_CreditsOWin()
    {
        var (store, id) = NewGame();
        store.Play(id, "X", 0, 0);
        store.Play(id, "O", 0, 1);
        store.Play(id, "X", 1, 2);
        store.Play(id, "O", 1, 1);
        store.Play(id, "X", 2, 0);
        var game = store.Play(id, "O", 2, 1);
        Assert.Equal("O", game.Winner);
        Assert.Equal(new[] { 1, 4, 7 }, game.WinningCells);
        Assert.Equal(1, store.GetScoreboard().OWins);
    }

    [Fact]
    public void DiagonalWin_Detected()
    {
        var (store, id) = NewGame();
        store.Play(id, "X", 0, 0);
        store.Play(id, "O", 0, 1);
        store.Play(id, "X", 1, 1);
        store.Play(id, "O", 0, 2);
        var game = store.Play(id, "X", 2, 2);
        Assert.Equal("Won", game.Status);
        Assert.Equal(new[] { 0, 4, 8 }, game.WinningCells);
    }

    [Fact]
    public void FullBoardNoWinner_IsDraw()
    {
        var (store, id) = NewGame();
        var sequence = new (string player, int row, int col)[]
        {
            ("X", 0, 0), ("O", 0, 1), ("X", 0, 2),
            ("O", 1, 1), ("X", 1, 0), ("O", 1, 2),
            ("X", 2, 1), ("O", 2, 0), ("X", 2, 2)
        };
        foreach (var (player, row, col) in sequence) store.Play(id, player, row, col);
        Assert.Equal("Draw", store.Get(id).Status);
        Assert.Null(store.Get(id).Winner);
        Assert.Equal(1, store.GetScoreboard().Draws);
    }

    [Fact]
    public void ResetClearsBoardAndHistoryButPreservesScoreboardAndGameId()
    {
        var (store, id) = NewGame();
        RowWin(store, id);
        var reset = store.Reset(id);
        Assert.Equal(id, reset.GameId);
        Assert.All(reset.Board, square => Assert.Null(square));
        Assert.Empty(reset.MoveHistory);
        Assert.Equal("X", reset.CurrentPlayer);
        Assert.Equal("InProgress", reset.Status);
        Assert.Null(reset.Winner);
        Assert.Equal(1, reset.Scoreboard.XWins);
    }

    [Fact]
    public void UndoTwoPlayer_RevertsExactlyOneMove()
    {
        var (store, id) = NewGame();
        store.Play(id, "X", 0, 0);
        store.Play(id, "O", 1, 1);
        var undo = store.Undo(id);
        Assert.Single(undo.MoveHistory);
        Assert.Null(undo.Board[4]);
        Assert.Equal("X", undo.Board[0]);
        Assert.Equal("O", undo.CurrentPlayer);
    }

    [Fact]
    public void UndoComputer_RevertsHumanAndComputerMoves()
    {
        var (store, id) = NewGame("Computer");
        var afterPlay = store.Play(id, "X", 0, 0);
        Assert.Equal("O", afterPlay.Board[4]);
        Assert.Equal(2, afterPlay.MoveHistory.Count);
        var undo = store.Undo(id);
        Assert.All(undo.Board, square => Assert.Null(square));
        Assert.Empty(undo.MoveHistory);
        Assert.Equal("X", undo.CurrentPlayer);
    }

    [Fact]
    public void UndoCannotRunWithNoMovesOrAfterCompletion()
    {
        var (store, id) = NewGame();
        Assert.Throws<GameRuleException>(() => store.Undo(id));
        RowWin(store, id);
        Assert.Throws<GameRuleException>(() => store.Undo(id));
        Assert.Equal(1, store.GetScoreboard().XWins);
    }

    [Fact]
    public void MoveAfterGameCompletionRejected_NoDoubleScore()
    {
        var (store, id) = NewGame();
        RowWin(store, id);
        Assert.Throws<GameRuleException>(() => store.Play(id, "O", 2, 2));
        Assert.Equal(5, store.Get(id).MoveHistory.Count);
        Assert.Equal(1, store.GetScoreboard().XWins);
    }

    [Fact]
    public void ScoreboardReset_DoesNotResetGame()
    {
        var (store, id) = NewGame();
        RowWin(store, id);
        var score = store.ResetScoreboard();
        Assert.Equal(0, score.XWins);
        Assert.Equal("Won", store.Get(id).Status);
        Assert.Equal(0, store.Get(id).Scoreboard.XWins);
    }

    [Fact]
    public void ComputerNeverMovesAfterHumanCompletesGame()
    {
        var (store, id) = NewGame("Computer");
        // X creates a fork and wins with the final move at (1,0).
        // The completed human move must not trigger another O turn.
        store.Play(id, "X", 0, 0);
        store.Play(id, "X", 2, 1);
        store.Play(id, "X", 2, 0);
        var won = store.Play(id, "X", 1, 0);
        Assert.Equal("Won", won.Status);
        Assert.Equal("X", won.Winner);
        Assert.Equal(7, won.MoveHistory.Count);
        Assert.Equal("X", won.MoveHistory[^1].Player);
        Assert.Equal(1, won.Scoreboard.XWins);
    }

    [Fact]
    public void ComputerModeRejectsClientOAndUsesValidUnoccupiedCells()
    {
        var (store, id) = NewGame("Computer");
        Assert.Throws<GameRuleException>(() => store.Play(id, "O", 1, 1));
        var result = store.Play(id, "X", 0, 0);
        Assert.Equal("O", result.Board[4]);
        Assert.Equal("X", result.CurrentPlayer);
    }

    [Fact]
    public void ComputerStrategyPrioritizesWinAboveBlock()
    {
        char?[] board = ['O', 'O', null, 'X', 'X', null, null, null, null];
        Assert.Equal(2, ComputerStrategy.ChooseMove(board));
    }

    [Fact]
    public void ComputerStrategyBlocksImmediateLoss()
    {
        char?[] board = ['X', 'X', null, null, 'O', null, null, null, null];
        Assert.Equal(2, ComputerStrategy.ChooseMove(board));
    }

    [Fact]
    public void ComputerStrategyUsesCenterCornerThenAnyFreeCell()
    {
        Assert.Equal(4, ComputerStrategy.ChooseMove(new char?[9]));
        char?[] corner = [null, null, null, null, 'X', null, null, null, null];
        Assert.Equal(0, ComputerStrategy.ChooseMove(corner));
        char?[] oneEmpty = ['X', null, 'X', 'X', 'X', 'O', 'O', 'X', 'O'];
        Assert.Equal(1, ComputerStrategy.ChooseMove(oneEmpty));
    }

    [Fact]
    public void SeparateGamesShareSessionScoreboard()
    {
        var store = new GameStore();
        var first = store.Create("TwoPlayer").GameId;
        RowWin(store, first);
        var second = store.Create("Computer");
        Assert.NotEqual(first, second.GameId);
        Assert.Equal(1, second.Scoreboard.XWins);
    }

    [Fact]
    public void MissingGameReturnsNotFoundException()
    {
        var store = new GameStore();
        Assert.Throws<KeyNotFoundException>(() => store.Get(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("Hard")]
    [InlineData("")]
    public void UnknownModeRejected(string mode)
    {
        Assert.Throws<GameRuleException>(() => new GameStore().Create(mode));
    }
}
