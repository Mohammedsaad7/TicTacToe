import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, finalize } from 'rxjs';
import { GameApiService, GameMode, GameState, Scoreboard } from './game-api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent implements OnInit {
  private readonly api = inject(GameApiService);

  readonly game = signal<GameState | null>(null);
  readonly selectedMode = signal<GameMode>('TwoPlayer');
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly scoreboard = computed<Scoreboard>(() =>
    this.game()?.scoreboard ?? { xWins: 0, oWins: 0, draws: 0 }
  );

  ngOnInit(): void {
    this.newGame('TwoPlayer');
  }

  newGame(mode: GameMode): void {
    if (this.busy()) return;
    this.execute(this.api.create(mode), result => {
      this.selectedMode.set(mode);
      this.game.set(result);
    });
  }

  selectMode(mode: GameMode): void {
    if (mode !== this.selectedMode()) this.newGame(mode);
  }

  play(index: number): void {
    const state = this.game();
    if (!state || this.busy() || state.status !== 'InProgress' || state.board[index]) return;
    if (state.mode === 'Computer' && state.currentPlayer !== 'X') return;
    const row = Math.floor(index / 3);
    const col = index % 3;
    this.execute(this.api.move(state.gameId, state.currentPlayer, row, col),
      next => this.game.set(next));
  }

  undo(): void {
    const state = this.game();
    if (!state || this.busy() || !state.canUndo) return;
    this.execute(this.api.undo(state.gameId), next => this.game.set(next));
  }

  resetGame(): void {
    const state = this.game();
    if (!state || this.busy()) return;
    this.execute(this.api.reset(state.gameId), next => this.game.set(next));
  }

  resetScoreboard(): void {
    const state = this.game();
    if (!state || this.busy()) return;
    this.execute(this.api.resetScoreboard(), score =>
      this.game.update(current => current ? { ...current, scoreboard: score } : null));
  }

  cellLabel(index: number, value: string | null): string {
    return `Row ${Math.floor(index / 3) + 1}, column ${index % 3 + 1}: ${value ?? 'empty'}`;
  }

  private execute<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.busy.set(true);
    this.error.set(null);
    request.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: onSuccess,
      error: (error: HttpErrorResponse) => {
        this.error.set(error.error?.detail ?? error.error?.title ??
          'Could not contact the API. Check that the backend is running on port 5057.');
      }
    });
  }
}
