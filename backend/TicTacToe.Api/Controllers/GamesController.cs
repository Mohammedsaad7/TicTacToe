using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(GameStore games) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status201Created)]
    public ActionResult<GameResponse> Create([FromBody] CreateGameRequest request)
    {
        var created = games.Create(request.Mode);
        return CreatedAtAction(nameof(Get), new { id = created.GameId }, created);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameResponse> Get(Guid id) => Ok(games.Get(id));

    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameResponse> Move(Guid id, [FromBody] PlayMoveRequest request) =>
        Ok(games.Play(id, request.Player, request.Row, request.Column));

    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameResponse> Undo(Guid id) => Ok(games.Undo(id));

    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameResponse> Reset(Guid id) => Ok(games.Reset(id));
}
