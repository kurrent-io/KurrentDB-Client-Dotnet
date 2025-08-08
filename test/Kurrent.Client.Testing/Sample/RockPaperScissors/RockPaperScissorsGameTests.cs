// // ReSharper disable CheckNamespace
//
// using Shouldly;
//
// namespace RockPaperScissors;
//
// public class RockPaperScissorsGameTests {
//     [Test]
//     public void game_can_be_started() {
//         // Arrange
//         var command = new StartGame(Guid.NewGuid(), 3);
//
//         var expectedEvents = new object[] {
//             new GameStarted(command.GameId, command.TotalRounds)
//         };
//
//         var expectedGameState = new Game {
//             Id          = command.GameId,
//             TotalRounds = command.TotalRounds,
//             Status      = GameStatus.AwaitingChoices,
//             Events      = expectedEvents.ToList()
//         };
//
//         var game = new Game();
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         events.ShouldHaveSingleItem();
//         events.ShouldBeEquivalentTo(expectedEvents);
//         actualGameState.ShouldBeEquivalentTo(expectedGameState);
//     }
//
//     [Test]
//     public void player_can_make_a_choice() {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3
//         };
//
//         var command = new MakeChoice(gameId, Player.Player1, Choice.Rock);
//
//         var expectedEvents = new object[] {
//             new ChoiceMade(gameId, Player.Player1, Choice.Rock)
//         };
//
//         var expectedRounds = new List<Round> {
//             new(1, Choice.Rock, null, null)
//         };
//
//         var expectedGameState = game with {
//             Rounds = expectedRounds,
//             Events = expectedEvents.ToList()
//         };
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         events.ShouldHaveSingleItem();
//         events.ShouldBeEquivalentTo(expectedEvents);
//         actualGameState.Rounds.ShouldBeEquivalentTo(expectedRounds);
//         actualGameState.Status.ShouldBe(GameStatus.AwaitingChoices);
//     }
//
//     [Test]
//     public void round_is_completed_when_both_players_make_choices() {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Rounds = [new Round(1, Choice.Rock, null, null)]
//         };
//
//         var command = new MakeChoice(gameId, Player.Player2, Choice.Scissors);
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         events.Length.ShouldBe(2); // ChoiceMade and RoundCompleted events
//
//         var choiceMadeEvent = events[0].ShouldBeOfType<ChoiceMade>();
//         choiceMadeEvent.GameId.ShouldBe(gameId);
//         choiceMadeEvent.Player.ShouldBe(Player.Player2);
//         choiceMadeEvent.Choice.ShouldBe(Choice.Scissors);
//
//         var roundCompletedEvent = events[1].ShouldBeOfType<RoundCompleted>();
//         roundCompletedEvent.GameId.ShouldBe(gameId);
//         roundCompletedEvent.RoundNumber.ShouldBe(1);
//         roundCompletedEvent.Player1Choice.ShouldBe(Choice.Rock);
//         roundCompletedEvent.Player2Choice.ShouldBe(Choice.Scissors);
//         roundCompletedEvent.Winner.ShouldBe(Player.Player1); // Rock beats Scissors
//
//         actualGameState.Player1Score.ShouldBe(1);
//         actualGameState.Player2Score.ShouldBe(0);
//         actualGameState.Rounds.Count.ShouldBe(1);
//         actualGameState.Rounds[0].Winner.ShouldBe(Player.Player1);
//     }
//
//     [Test]
//     public void player_cannot_make_choice_twice_in_same_round() {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Rounds = [new Round(1, Choice.Rock, null, null)]
//         };
//
//         var command = new MakeChoice(gameId, Player.Player1, Choice.Paper);
//
//         // Act & Assert
//         Should.Throw<InvalidOperationException>(() => game.Execute(command))
//             .Message.ShouldBe("Player 1 has already made a choice");
//     }
//
//     [Test]
//     public void round_is_draw_when_both_players_make_same_choice() {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Rounds = [new Round(1, Choice.Rock, null, null)]
//         };
//
//         var command = new MakeChoice(gameId, Player.Player2, Choice.Rock);
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         var roundCompletedEvent = events[1].ShouldBeOfType<RoundCompleted>();
//         roundCompletedEvent.Winner.ShouldBeNull(); // Draw
//         actualGameState.Player1Score.ShouldBe(0);
//         actualGameState.Player2Score.ShouldBe(0);
//     }
//
//     [Test]
//     public void game_is_won_when_player_wins_majority_of_rounds() {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Rounds = [
//                 new Round(1, Choice.Rock, Choice.Scissors, Player.Player1),
//                 new Round(2, Choice.Paper, Choice.Rock, Player.Player1)
//             ],
//             Player1Score = 2,
//             Player2Score = 0
//         };
//
//         // When Player1 already won 2 out of 3 rounds, game should be over after this move
//         var command = new MakeChoice(gameId, Player.Player1, Choice.Rock);
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         events.Length.ShouldBe(2); // ChoiceMade and GameWon events
//         var gameWonEvent = events[1].ShouldBeOfType<GameWon>();
//         gameWonEvent.Winner.ShouldBe(Player.Player1);
//         gameWonEvent.Player1Score.ShouldBe(2);
//         gameWonEvent.Player2Score.ShouldBe(0);
//
//         actualGameState.Status.ShouldBe(GameStatus.Player1Won);
//         actualGameState.Winner.ShouldBe(Player.Player1);
//     }
//
//     [Test]
//     public void game_is_draw_when_players_win_equal_number_of_rounds() {
//         // Arrange - create a new game
//         var gameId = Guid.NewGuid();
//         var game = new Game();
//
//         // Start the game
//         var startGameCommand = new StartGame(gameId, 3);
//         (game, _) = game.Execute(startGameCommand);
//
//         // Round 1: Player1 wins (Rock beats Scissors)
//         var player1Round1Command = new MakeChoice(gameId, Player.Player1, Choice.Rock);
//         (game, _) = game.Execute(player1Round1Command);
//
//         var player2Round1Command = new MakeChoice(gameId, Player.Player2, Choice.Scissors);
//         (game, _) = game.Execute(player2Round1Command);
//
//         // Round 2: Player2 wins (Scissors beats Paper)
//         var player1Round2Command = new MakeChoice(gameId, Player.Player1, Choice.Paper);
//         (game, _) = game.Execute(player1Round2Command);
//
//         var player2Round2Command = new MakeChoice(gameId, Player.Player2, Choice.Scissors);
//         (game, _) = game.Execute(player2Round2Command);
//
//         // Round 3: Player1 makes a choice (Rock)
//         var player1Round3Command = new MakeChoice(gameId, Player.Player1, Choice.Rock);
//         (game, _) = game.Execute(player1Round3Command);
//
//         // Round 3: Player2 makes same choice, resulting in draw
//         var player2Round3Command = new MakeChoice(gameId, Player.Player2, Choice.Rock);
//
//         // Act - complete the final round which should result in a game draw
//         var (actualGameState, events) = game.Execute(player2Round3Command);
//
//         // Assert
//         events.Length.ShouldBe(2); // ChoiceMade and GameDraw events
//         var choiceMadeEvent = events[0].ShouldBeOfType<ChoiceMade>();
//         choiceMadeEvent.GameId.ShouldBe(gameId);
//         choiceMadeEvent.Player.ShouldBe(Player.Player2);
//         choiceMadeEvent.Choice.ShouldBe(Choice.Rock);
//
//         var gameDrawEvent = events[1].ShouldBeOfType<GameDraw>();
//         gameDrawEvent.GameId.ShouldBe(gameId);
//         gameDrawEvent.Score.ShouldBe(1); // Both players have 1 point
//
//         actualGameState.Status.ShouldBe(GameStatus.Draw);
//         actualGameState.Winner.ShouldBeNull();
//         actualGameState.Player1Score.ShouldBe(1);
//         actualGameState.Player2Score.ShouldBe(1);
//     }
//
//     [Test]
//     [Arguments(Choice.Rock, Choice.Scissors, Player.Player1)]
//     [Arguments(Choice.Paper, Choice.Rock, Player.Player1)]
//     [Arguments(Choice.Scissors, Choice.Paper, Player.Player1)]
//     [Arguments(Choice.Scissors, Choice.Rock, Player.Player2)]
//     [Arguments(Choice.Rock, Choice.Paper, Player.Player2)]
//     [Arguments(Choice.Paper, Choice.Scissors, Player.Player2)]
//     public void game_rules_determine_correct_winner(Choice player1Choice, Choice player2Choice, Player expectedWinner) {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Rounds = [new Round(1, player1Choice, null, null)]
//         };
//
//         var command = new MakeChoice(gameId, Player.Player2, player2Choice);
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         var roundCompletedEvent = events[1].ShouldBeOfType<RoundCompleted>();
//         roundCompletedEvent.Winner.ShouldBe(expectedWinner);
//         actualGameState.Rounds[0].Winner.ShouldBe(expectedWinner);
//     }
//
//     [Test]
//     [Arguments(Choice.Rock, Choice.Rock)]
//     [Arguments(Choice.Paper, Choice.Paper)]
//     [Arguments(Choice.Scissors, Choice.Scissors)]
//     public void identical_choices_result_in_draw(Choice player1Choice, Choice player2Choice) {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Rounds = [new Round(1, player1Choice, null, null)]
//         };
//
//         var command = new MakeChoice(gameId, Player.Player2, player2Choice);
//
//         // Act
//         var (actualGameState, events) = game.Execute(command);
//
//         // Assert
//         var roundCompletedEvent = events[1].ShouldBeOfType<RoundCompleted>();
//         roundCompletedEvent.Winner.ShouldBeNull();
//         actualGameState.Rounds[0].Winner.ShouldBeNull();
//     }
//
//     [Test]
//     public void cannot_make_choices_after_all_rounds_completed() {
//         // Arrange
//         var gameId = Guid.NewGuid();
//         var game = new Game {
//             Id = gameId,
//             TotalRounds = 3,
//             Status = GameStatus.Player1Won,
//             Rounds = [
//                 new Round(1, Choice.Rock, Choice.Scissors, Player.Player1),
//                 new Round(2, Choice.Paper, Choice.Rock, Player.Player1),
//                 new Round(3, Choice.Scissors, Choice.Paper, Player.Player1)
//             ],
//             Player1Score = 3,
//             Player2Score = 0,
//             Winner = Player.Player1
//         };
//
//         var command = new MakeChoice(gameId, Player.Player1, Choice.Rock);
//
//         // Act & Assert
//         Should.Throw<InvalidOperationException>(() => game.Execute(command))
//             .Message.ShouldBe("Game is complete. All 3 rounds have been played");
//     }
// }
