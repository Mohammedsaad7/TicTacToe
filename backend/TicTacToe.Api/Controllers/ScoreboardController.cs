using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController(GameStore games) : ControllerBase
{
    [HttpGet]
    public ActionResult<ScoreboardResponse> Get() => Ok(games.GetScoreboard());

    [HttpPost("reset")]
    public ActionResult<ScoreboardResponse> Reset() => Ok(games.ResetScoreboard());
}
