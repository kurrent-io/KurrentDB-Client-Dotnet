// ReSharper disable CheckNamespace

namespace RockPaperScissors;

public static class RockPaperScissorsSimulator {
    /// <summary>
    /// Simulates a complete Rock-Paper-Scissors game from start to finish.
    /// Players make random choices for each round.
    /// </summary>
    /// <param name="totalRounds">Number of rounds to play (default 3)</param>
    /// <returns>A tuple containing the game ID, list of game events, and the winner (null if draw)</returns>
    public static (Guid GameId, List<object> Events, Player? Winner) SimulateGame(int totalRounds = 3) {
        var gameId = Guid.NewGuid();
        var game = new Game();

        // 1. Start the game
        var startGameCmd = new StartGame(gameId, totalRounds);
        (game, _) = game.Execute(startGameCmd);

        // 2. Play rounds until game completes
        for (int round = 1; round <= totalRounds; round++) {
            // Player 1 makes a choice
            var player1Choice = GetRandomChoice();
            var player1Cmd = new MakeChoice(gameId, Player.Player1, player1Choice);
            (game, _) = game.Execute(player1Cmd);

            // Player 2 makes a choice
            var player2Choice = GetRandomChoice();
            var player2Cmd = new MakeChoice(gameId, Player.Player2, player2Choice);
            (game, _) = game.Execute(player2Cmd);

            // If game has a winner already (majority win), stop simulation
            if (game.Status != GameStatus.AwaitingChoices)
                break;
        }

        // Return the final game state information
        return (game.Id, game.Events, game.Winner);
    }

    /// <summary>
    /// Gets a random choice (Rock, Paper, or Scissors)
    /// </summary>
    private static Choice GetRandomChoice() {
        return (Choice)Random.Shared.Next(0, 3); // 0=Rock, 1=Paper, 2=Scissors
    }
}
