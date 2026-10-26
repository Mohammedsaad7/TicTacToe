namespace TicTacToe.Api.Contracts;

public sealed record CreateGameRequest(string Mode = "TwoPlayer");
public sealed record PlayMoveRequest(string Player, int Row, int Column);
public sealed record MoveResponse(int Number, string Player, int Row, int Column);
public sealed record ScoreboardResponse(int XWins, int OWins, int Draws);
public sealed record GameResponse(
    Guid GameId,
    string?[] Board,
    string CurrentPlayer,
    string Mode,
    string Status,
    string? Winner,
    int[] WinningCells,
    IReadOnlyList<MoveResponse> MoveHistory,
    bool CanUndo,
    ScoreboardResponse Scoreboard);
