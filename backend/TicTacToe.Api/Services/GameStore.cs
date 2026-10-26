using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Services;

/// <summary>
/// Process-wide, in-memory store. Lock makes a human move + computer response +
/// scoreboard update a single atomic operation.
/// </summary>
public sealed class GameStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, GameSession> _games = [];
    private int _xWins;
    private int _oWins;
    private int _draws;

    public GameResponse Create(string? mode)
    {
        GameMode parsed;
        if (string.Equals(mode, "TwoPlayer", StringComparison.OrdinalIgnoreCase))
            parsed = GameMode.TwoPlayer;
        else if (string.Equals(mode, "Computer", StringComparison.OrdinalIgnoreCase))
            parsed = GameMode.Computer;
        else
            throw new GameRuleException("Mode must be TwoPlayer or Computer.");

        lock (_gate)
        {
            var session = new GameSession(parsed);
            _games.Add(session.Id, session);
            return ToResponse(session);
        }
    }

    public GameResponse Get(Guid id)
    {
        lock (_gate) return ToResponse(Find(id));
    }

    public GameResponse Play(Guid id, string? player, int row, int column)
    {
        lock (_gate)
        {
            var game = Find(id);
            if (player is not ("X" or "O"))
                throw new GameRuleException("Player must be X or O.");
            // A failed move doesn't mutate session, move history or scoreboard.
            game.Play(player[0], row, column);
            if (game.Status == GameStatus.Won)
            {
                if (game.Winner == 'X') _xWins++;
                else _oWins++;
            }
            else if (game.Status == GameStatus.Draw) _draws++;
            return ToResponse(game);
        }
    }

    public GameResponse Undo(Guid id)
    {
        lock (_gate)
        {
            var game = Find(id);
            game.Undo();
            return ToResponse(game);
        }
    }

    public GameResponse Reset(Guid id)
    {
        lock (_gate)
        {
            var game = Find(id);
            // Game ID is retained; it represents the resettable game slot.
            game.Reset();
            return ToResponse(game);
        }
    }

    public ScoreboardResponse GetScoreboard()
    {
        lock (_gate) return NewScoreboard();
    }

    public ScoreboardResponse ResetScoreboard()
    {
        lock (_gate)
        {
            _xWins = _oWins = _draws = 0;
            return NewScoreboard();
        }
    }

    private GameSession Find(Guid id) => _games.TryGetValue(id, out var game)
        ? game : throw new KeyNotFoundException($"Game {id} was not found.");

    private ScoreboardResponse NewScoreboard() => new(_xWins, _oWins, _draws);

    private GameResponse ToResponse(GameSession game) => new(
        game.Id,
        game.Board.Select(cell => cell?.ToString()).ToArray(),
        game.CurrentPlayer.ToString(),
        game.Mode.ToString(),
        game.Status.ToString(),
        game.Winner?.ToString(),
        [.. game.WinningCells],
        game.History.Select(m => new MoveResponse(m.Number, m.Player.ToString(), m.Row, m.Column)).ToArray(),
        game.Status == GameStatus.InProgress && game.History.Count > 0,
        NewScoreboard());
}
