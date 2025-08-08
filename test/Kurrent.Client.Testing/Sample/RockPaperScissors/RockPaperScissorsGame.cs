// ReSharper disable CheckNamespace

namespace RockPaperScissors;

public record Game {
    public Guid         Id           { get; init; }
    public int          TotalRounds  { get; init; } = 3;
    public List<Round>  Rounds       { get; init; } = [];
    public GameStatus   Status       { get; init; } = GameStatus.AwaitingChoices;
    public int          Player1Score { get; init; } = 0;
    public int          Player2Score { get; init; } = 0;
    public Player?      Winner       { get; init; }
    public List<object> Events       { get; init; } = [];

    public (Game UpdatedGame, object[] Events) Execute(object command) {
        var result = command switch {
            StartGame cmd  => StartNewGame(cmd),
            MakeChoice cmd => MakePlayerChoice(cmd),
            _              => throw new InvalidOperationException("Invalid command")
        };

        // Track events in the game state
        var (game, events) = result;
        var updatedEvents = new List<object>(game.Events);
        updatedEvents.AddRange(events);

        return (game with { Events = updatedEvents }, events);
    }

    (Game, object[]) StartNewGame(StartGame cmd) {
        if (Rounds.Count > 0)
            throw new InvalidOperationException("Game has already started");

        var gameStartedEvent = new GameStarted(cmd.GameId, cmd.TotalRounds);
        return (this with { Id = cmd.GameId, TotalRounds = cmd.TotalRounds }, [gameStartedEvent]);
    }

    (Game, object[]) MakePlayerChoice(MakeChoice cmd) {
        // Validate command
        if (Status != GameStatus.AwaitingChoices &&
            Status != GameStatus.Player1Won &&
            Status != GameStatus.Player2Won)
            throw new InvalidOperationException("Cannot make a choice when the game is over");

        // Determine current round
        var currentRoundNumber = Rounds.Count;
        if (currentRoundNumber >= TotalRounds)
            throw new InvalidOperationException($"Game is complete. All {TotalRounds} rounds have been played");

        // Get current round or create new one
        var currentRound = currentRoundNumber > 0 &&
                           !IsRoundComplete(Rounds[^1])
            ? Rounds[^1]
            : new Round(
                currentRoundNumber + 1, null, null,
                null
            );

        // Update round with player's choice
        var updatedRound = cmd.Player switch {
            Player.Player1 when currentRound.Player1Choice != null =>
                throw new InvalidOperationException("Player 1 has already made a choice"),
            Player.Player2 when currentRound.Player2Choice != null =>
                throw new InvalidOperationException("Player 2 has already made a choice"),
            Player.Player1 => currentRound with { Player1Choice = cmd.Choice },
            Player.Player2 => currentRound with { Player2Choice = cmd.Choice },
            _              => throw new ArgumentOutOfRangeException(nameof(cmd.Player), "Invalid player value")
        };

        // Create choice made event
        var events = new List<object> { new ChoiceMade(Id, cmd.Player, cmd.Choice) };

        // Check if round is complete (both players made choices)
        if (updatedRound.Player1Choice != null && updatedRound.Player2Choice != null) {
            var roundWinner = DetermineRoundWinner(updatedRound.Player1Choice.Value, updatedRound.Player2Choice.Value);
            updatedRound = updatedRound with { Winner = roundWinner };

            // Add round completed event
            events.Add(
                new RoundCompleted(
                    Id,
                    updatedRound.RoundNumber,
                    updatedRound.Player1Choice.Value,
                    updatedRound.Player2Choice.Value,
                    roundWinner
                )
            );
        }

        // Update game rounds
        List<Round> updatedRounds;
        if (currentRoundNumber > 0 && !IsRoundComplete(Rounds[^1])) {
            // Update current in-progress round
            updatedRounds     = new List<Round>(Rounds);
            updatedRounds[^1] = updatedRound;
        }
        else {
            // Add new round
            updatedRounds = new List<Round>(Rounds) { updatedRound };
        }

        // Calculate scores and check for game completion
        var (player1Score, player2Score) = CalculateScores(updatedRounds);
        var newGame = this with {
            Rounds = updatedRounds,
            Player1Score = player1Score,
            Player2Score = player2Score
        };

        // Handle game completion if necessary
        if (IsGameComplete(updatedRounds, player1Score, player2Score, out var gameWinner)) {
            var gameStatus = gameWinner switch {
                null           => GameStatus.Draw,
                Player.Player1 => GameStatus.Player1Won,
                _              => GameStatus.Player2Won
            };

            if (gameStatus == GameStatus.Draw)
                events.Add(new GameDraw(Id, player1Score)); // Scores are equal
            else
                events.Add(new GameWon(Id, gameWinner!.Value, player1Score, player2Score));

            newGame = newGame with { Status = gameStatus, Winner = gameWinner };
        }

        return (newGame, events.ToArray());
    }

    static bool IsRoundComplete(Round round) => round is { Player1Choice: not null, Player2Choice: not null };

    // Game rules for determining a winner
    static Player? DetermineRoundWinner(Choice player1Choice, Choice player2Choice) {
        if (player1Choice == player2Choice)
            return null; // Draw

        return (player1Choice, player2Choice) switch {
            (Choice.Rock, Choice.Scissors)  => Player.Player1,
            (Choice.Paper, Choice.Rock)     => Player.Player1,
            (Choice.Scissors, Choice.Paper) => Player.Player1,
            _                               => Player.Player2
        };
    }

    static (int Player1Score, int Player2Score) CalculateScores(List<Round> rounds) {
        int player1Score = 0, player2Score = 0;

        foreach (var round in rounds.Where(IsRoundComplete))
            if (round.Winner == Player.Player1)
                player1Score++;
            else if (round.Winner == Player.Player2)
                player2Score++;

        return (player1Score, player2Score);
    }

    bool IsGameComplete(List<Round> rounds, int player1Score, int player2Score, out Player? winner) {
        // Game is done if all rounds completed or if one player has mathematically won
        var allRoundsCompleted = rounds.Count(IsRoundComplete) >= TotalRounds;

        // Check if either player has a majority of wins
        var remainingRounds  = TotalRounds - rounds.Count(IsRoundComplete);
        var player1CannotWin = player1Score + remainingRounds < player2Score;
        var player2CannotWin = player2Score + remainingRounds < player1Score;

        if (allRoundsCompleted || player1CannotWin || player2CannotWin) {
            if (player1Score > player2Score)
                winner = Player.Player1;
            else if (player2Score > player1Score)
                winner = Player.Player2;
            else
                winner = null; // Draw

            return true;
        }

        winner = null;
        return false;
    }
}
