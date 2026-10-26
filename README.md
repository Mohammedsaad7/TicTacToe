# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe implementation for an interview take-home exercise. The Angular frontend sends all game actions to a .NET Web API; the API owns the board, rules, move history, computer decisions, and session scoreboard.

**Technologies:** Angular 21 (TypeScript, standalone components, signals), .NET 10 Web API (C#), REST/JSON, in-memory singleton store, xUnit. No database, Docker, or cloud account is required.

## Quick start

Install **.NET SDK 10** and **Node.js 22.12+** (or another compatible Node 20.19+/24+ release), with npm.

**Terminal 1 — API** (from the repository root):

```bash
dotnet run --project backend/TicTacToe.Api --launch-profile http
```

API: `http://localhost:5057` · health: `http://localhost:5057/health`

**Terminal 2 — Angular** (from the repository root):

```bash
cd frontend
npm install
npm start
```

Open `http://localhost:4200` in your browser. `npm start` uses `proxy.conf.json` to route `/api` requests to the .NET API on port 5057; no production deployment or special CORS config is necessary. The API additionally permits requests from `http://localhost:4200`.

**Important:** The backend must be running before you load the frontend. The first page load creates a new Two Player game by calling the API.

### Windows PowerShell

The same commands work in two PowerShell terminals. Install .NET SDK 10 from Microsoft's official .NET website, and Node.js from nodejs.org. If another app occupies port 5057 or 4200, stop it first or update the backend URL, proxy, and start script together.

## Features

- 3×3 click-to-play board for **Two Player** and **vs. Computer** modes.
- Server-side validation of turn, position, empty cell, and game status; invalid requests return HTTP 400 with a `ProblemDetails` JSON body and change no state.
- Horizontal, vertical and diagonal wins; winning squares highlighted; draws detected.
- Player turn, result messaging, per-move history (number, player, zero-based coordinates served by API / one-based on UI).
- Two-player undo (one move) and computer-mode undo (human + computer pair).
- Win > block > center > corners in ascending index > first available square computer heuristic. Human always plays X.
- Reset Game clears board and history without affecting accumulated scores.
- Server-side session scoreboard tracks X wins, O wins, and draws; independent Reset Scoreboard action.
- Responsive frontend with keyboard-focus indicators and per-cell accessible labels.

## API contract

All data are JSON. ASP.NET Core serializes the C# contract properties as camelCase, e.g. `gameId`, `currentPlayer`, and `winningCells`.

| Method | Path | Request body | Response | Purpose |
|---|---|---|---|---|
| POST | `/api/games` | `{ "mode": "TwoPlayer" }` or `{ "mode": "Computer" }` | `201 GameState`, `Location` header | Create game |
| GET | `/api/games/{id}` | — | `200 GameState` | Fetch authoritative state |
| POST | `/api/games/{id}/moves` | `{ "player": "X", "row": 0, "column": 0 }` | `200 GameState` | Submit a valid turn (also plays O in computer mode) |
| POST | `/api/games/{id}/undo` | `{}` | `200 GameState` | Undo according to game mode |
| POST | `/api/games/{id}/reset` | `{}` | `200 GameState` | Reset board/moves, retain ID and scoreboard |
| GET | `/api/scoreboard` | — | `200 Scoreboard` | Fetch accumulated score |
| POST | `/api/scoreboard/reset` | `{}` | `200 Scoreboard` | Zero all scores |
| GET | `/health` | — | `{ "status": "ok" }` | Health check |

For invalid actions, the response is `400 application/problem+json`; unknown game IDs return `404 application/problem+json`. Board coordinates are **0-based** (`row`/`column` = 0, 1, or 2), with row-major cell indices `0..8`.

### Sample state

```json
{
  "gameId": "0dff8146-fb0b-423e-bc06-773bcc8744a3",
  "board": ["X", null, null, null, "O", null, null, null, null],
  "currentPlayer": "X",
  "mode": "Computer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moveHistory": [
    { "number": 1, "player": "X", "row": 0, "column": 0 },
    { "number": 2, "player": "O", "row": 1, "column": 1 }
  ],
  "canUndo": true,
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

In Two Player mode the client submits whichever player `currentPlayer` specifies; in Computer mode it only submits `X`. The game ID is in the route (it does not need to be duplicated in the body). `winningCells` contains **0-based indices** of the winning row, column, or diagonal. `status` is `InProgress`, `Won`, or `Draw`.

## Run tests

```bash
dotnet test backend/TicTacToe.Tests/TicTacToe.Tests.csproj
```

Backend xUnit tests cover valid and invalid moves, turn switching, occupied cells, out-of-range moves, row/column/diagonal wins, draws, score changes, reset, both undo modes, terminal-state protections, independent game sessions, and computer move priority. The Angular package also has `npm run typecheck` and `npm run build` for frontend verification.

## Architecture and design decisions

```
frontend/src/app/
  app.component.ts/.html/.css   UI + signals / async request handling
  game-api.service.ts           Typed REST client and DTOs
backend/TicTacToe.Api/
  Controllers/                  REST endpoints
  Contracts/                    API response/request DTOs
  Domain/GameSession.cs         Board, turn, status, validation, undo, reset
  Domain/ComputerStrategy.cs    AI move selection
  Services/GameStore.cs         In-memory sessions, locking, scoreboard
backend/TicTacToe.Tests/        xUnit regression tests
```

**Backend owns the truth.** The frontend never computes a winner or AI move, never writes directly to the scoreboard, and always replaces its state from API responses. The UI disables obvious invalid actions for ergonomics, but the backend independently enforces every rule.

**Atomic computer turn.** In computer mode the move endpoint applies X, checks for a terminal result, and (only if still in progress) applies O before returning. This prevents user actions interleaving with the opponent and makes undo straightforward. The heuristic is deterministic rather than perfect minimax.

**Option A — no undo after completion.** Undo is rejected once the game is won or drawn (UI also disables it). Every completed game increments the scoreboard exactly once; repeating a move on a terminal game returns 400. Scores can be cleared separately without reopening finished games.

**Concurrency.** The singleton `GameStore` serializes mutations with one `lock`: two requests cannot both occupy the same cell, bypass alternating turns, or double-count results. A single process-wide scoreboard is shared across all game IDs while the server runs; it isn't tied to a browser tab or a user account.

**Reset semantics.** `POST /api/games/{id}/reset` starts a new *logical round* under the existing ID. Switching game mode creates a new ID. This keeps the endpoint simple, retains the scoreboard, and avoids broken references. Reset Scoreboard does not change game boards, even if a game was previously completed.

**Storage.** For this exercise, a singleton dictionary is sufficient and avoids setup steps. Restarting the backend clears all games and scores. Game IDs that are abandoned remain in memory until the process exits.

## Assumptions and clarifications

- Human X always moves first; in computer mode O is always automated.
- The user cannot change mode within an active game: choosing another mode creates a new game and discards the currently displayed board; existing server game IDs remain accessible until restart.
- Coordinates are zero-based in API requests/responses and one-based in human-facing move history.
- A move pair is one atomic client request in computer mode; no separate `/computer-move` action is needed.
- New game and Reset Game preserve the scoreboard; Reset Scoreboard resets scores across all games.
- The scoreboard represents the **current backend process**, not a per-user authenticated profile.
- There is no undo or redo for a finished round (specification's Option A).

## AI-assisted development notes

This submission draft was produced with ChatGPT as an AI-assisted implementation. **Actual user prompt:** the full exercise specification supplied in the chat, from "Problem Statement" through "Acceptance Criteria" (the same requirements implemented here). No separate follow-up coding prompts were provided by the user during generation. The following were the key *implementation subtasks*, not verbatim user prompts:

1. Extract acceptance criteria and choose server ownership and undo Option A.
2. Implement game rules, session store, deterministic computer move priority, and atomic move pairs.
3. Expose typed REST contracts and create an Angular UI driven by responses.
4. Add xUnit cases and document design, assumptions, known limitations, and local run instructions.

**AI generated:** API/controllers, domain rules, tests, Angular UI/service/styles, and this README. **Review performed in this chat:** static TypeScript syntax, JSON configuration validity, and a manual reasoning check of core move/AI sequences. **Not performed:** a compiled .NET test run or a complete Angular build. **Candidate responsibility:** personally review the rule engine, lock scope, AI move priorities, scoreboard semantics, UI behavior, and tests; document any changes made yourself. Do not claim manual work or test passes that have not actually happened.

## Known limitations

- Backend state/scoreboard disappears on API restart; no persistence or authentication.
- A shared process-wide scoreboard is unsuitable for true multi-user production use.
- In-memory game sessions aren't automatically expired; no storage eviction.
- AI is rule-based rather than unbeatable; ties use a fixed deterministic order.
- API is HTTP-only for convenient localhost review; a deployed application should use HTTPS, per-user sessions, production CORS, and appropriate observability.
- No E2E/UI automation or retry mechanism. Backend xUnit test coverage and frontend build verification are the focus.
- **Execution note:** The authoring environment used for this generated archive did not have the .NET SDK or access to download npm packages. The projects were written but an actual `dotnet test` / `npm install` / `ng build` run could not be completed here. Run the commands above on a development machine before submitting and fix any environment-specific issues.

## Future improvements

SQLite (and game expiry), per-player session authentication, an OpenAPI/Swagger document, integration tests with `WebApplicationFactory`, browser end-to-end tests, minimax difficulty settings, and persistence across refreshes.
