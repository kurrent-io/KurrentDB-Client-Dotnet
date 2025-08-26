// ReSharper disable CheckNamespace

namespace RockPaperScissors;

public enum Player {
    Player1, Player2
}

public enum Choice {
    Rock,
    Paper,
    Scissors
}

public enum GameStatus {
    AwaitingChoices,
    Player1Won,
    Player2Won,
    Draw
}

public record Round(int RoundNumber, Choice? Player1Choice, Choice? Player2Choice, Player? Winner);

// Commands
public record StartGame(Guid GameId, int TotalRounds = 3);

public record MakeChoice(Guid GameId, Player Player, Choice Choice);

// Events
public record GameStarted(Guid GameId, int TotalRounds);

public record ChoiceMade(Guid GameId, Player Player, Choice Choice);

public record RoundCompleted(Guid GameId, int RoundNumber, Choice Player1Choice, Choice Player2Choice, Player? Winner);

public record GameWon(Guid GameId, Player Winner, int Player1Score, int Player2Score);

public record GameDraw(Guid GameId, int Score);
