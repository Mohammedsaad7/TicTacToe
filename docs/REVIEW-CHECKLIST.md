# Panel review & local validation checklist

This list is meant to be run **after installing .NET 10 and npm dependencies**.

## Before submitting

- [ ] `dotnet build backend/TicTacToe.sln` succeeds.
- [ ] `dotnet test backend/TicTacToe.sln` succeeds.
- [ ] `cd frontend && npm install && npm run typecheck && npm run build` succeeds.
- [ ] With API and Angular running, opening `http://localhost:4200` creates a game.
- [ ] In Two Player mode, X/O alternate and both occupied and out-of-bounds requests return 400 without changing turns.
- [ ] Check row, column, diagonal win, final-score increment, and highlight.
- [ ] Check 9-move draw; scoreboard Draws increments once.
- [ ] Check Undo removes one move in Two Player mode and both moves in Computer mode.
- [ ] Check Undo disabled after game ends (Option A).
- [ ] Check Reset Game clears board, retains scoreboard; Reset Scoreboard zeros just scores.
- [ ] Check Computer picks center, blocks a threat, and takes a win before blocking.
- [ ] Check laptop viewport and keyboard navigation.
- [ ] Create an empty GitHub repo, push this folder, and confirm README renders correctly.

## Useful questions to practice

- Why is the backend the source of truth?
- How does one `POST /moves` result in two moves in Computer mode?
- Which validations happen before mutating the board?
- Why is scoring performed in `GameStore.Play` rather than Angular?
- How does `lock` prevent concurrent moves from corrupting history or double-counting a result?
- Why pick undo Option A? How would you implement Option B safely?
- What changes for distributed deployment or multi-user scoreboard isolation?
- What cases do your xUnit tests exercise? Which gaps would integration/E2E tests address?

## GitHub push (after creating your own empty remote repository)

```bash
git init
git add .
git commit -m "Build Angular and .NET Tic Tac Toe"
git branch -M main
git remote add origin https://github.com/<YOUR-USER>/<YOUR-REPO>.git
git push -u origin main
```

Replace the placeholders with your own GitHub account and repository; no remote is configured in this archive.
