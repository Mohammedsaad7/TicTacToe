using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5057");
builder.Services.AddControllers();
builder.Services.AddSingleton<GameStore>();
builder.Services.AddCors(options => options.AddPolicy("LocalAngular", policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = exception switch
    {
        GameRuleException => StatusCodes.Status400BadRequest,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError
    };
    await Results.Problem(
        statusCode: status,
        title: status switch
        {
            400 => "Invalid game operation",
            404 => "Game not found",
            _ => "Unexpected server error"
        },
        detail: status == 500 ? "An unexpected error occurred." : exception?.Message
    ).ExecuteAsync(context);
}));

app.UseCors("LocalAngular");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

// Lets integration tests reference Program, if later needed.
public partial class Program { }
