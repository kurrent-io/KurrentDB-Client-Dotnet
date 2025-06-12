using TicTacToe;

namespace Kurrent.Client.Tests.Infra.Result;

public readonly record struct GameId(Guid Value);

public record PlayerTurn(Player Player, Position Position, string? MoveDescription = null);

public record GameUpdated(Guid GameId, GameStatus NewStatus, string? UpdateNotes = null);

public record InvalidMoveError(Guid GameId, string Reason, string? StateContext = null);

public record GameEndedError(Guid GameId, GameStatus Status, string? FinalStateContext = null);
