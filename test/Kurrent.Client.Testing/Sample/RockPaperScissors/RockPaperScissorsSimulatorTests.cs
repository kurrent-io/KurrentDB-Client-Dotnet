// // ReSharper disable CheckNamespace
//
// using Shouldly;
//
// namespace RockPaperScissors;
//
// public class RockPaperScissorsSimulatorTests {
//     [Test]
//     public void generates_a_non_empty_list_of_events_for_a_simulated_game() {
//         // Act
//         var (_, events, _) = RockPaperScissorsSimulator.SimulateGame();
//
//         // Assert
//         events.ShouldNotBeEmpty();
//     }
//
//     [Test]
//     public void first_event_is_always_game_started_with_valid_details() {
//         // Act
//         var (gameId, events, _) = RockPaperScissorsSimulator.SimulateGame(3);
//
//         // Assert
//         var gameStartedEvent = events.First().ShouldBeOfType<GameStarted>();
//         gameStartedEvent.GameId.ShouldBe(gameId);
//         gameStartedEvent.GameId.ShouldNotBe(Guid.Empty);
//         gameStartedEvent.TotalRounds.ShouldBe(3);
//     }
//
//     [Test]
//     public void last_event_is_always_game_won_or_game_draw() {
//         // Act
//         var (_, events, _) = RockPaperScissorsSimulator.SimulateGame();
//
//         // Assert
//         (events.Last() is GameWon or GameDraw).ShouldBeTrue();
//     }
//
//     [Test]
//     public void choice_made_events_alternate_between_players() {
//         // Act
//         var (_, events, _) = RockPaperScissorsSimulator.SimulateGame();
//         var choiceMadeEvents = events.OfType<ChoiceMade>().ToList();
//
//         // Assert
//         choiceMadeEvents.ShouldNotBeEmpty();
//
//         for (int i = 0; i < choiceMadeEvents.Count; i++) {
//             var expectedPlayer = i % 2 == 0 ? Player.Player1 : Player.Player2;
//             choiceMadeEvents[i].Player.ShouldBe(expectedPlayer);
//         }
//     }
//
//     [Test]
//     public void round_completed_events_have_valid_data() {
//         // Act
//         var (_, events, _) = RockPaperScissorsSimulator.SimulateGame();
//         var roundCompletedEvents = events.OfType<RoundCompleted>().ToList();
//
//         // Assert
//         roundCompletedEvents.ShouldNotBeEmpty();
//
//         foreach (var roundEvent in roundCompletedEvents) {
//             roundEvent.RoundNumber.ShouldBeGreaterThan(0);
//
//             // Winner should be consistent with the choices based on game rules
//             if (roundEvent.Player1Choice == roundEvent.Player2Choice) {
//                 // Draw
//                 roundEvent.Winner.ShouldBeNull();
//             } else {
//                 var expectedWinner = DetermineExpectedWinner(roundEvent.Player1Choice, roundEvent.Player2Choice);
//                 roundEvent.Winner.ShouldBe(expectedWinner);
//             }
//         }
//     }
//
//     [Test]
//     public void simulated_game_has_correct_number_of_events() {
//         // Arrange
//         const int totalRounds = 3;
//
//         // Act
//         var (_, events, _) = RockPaperScissorsSimulator.SimulateGame(totalRounds);
//
//         // Assert
//         var gameStartedCount = events.OfType<GameStarted>().Count();
//         var choiceMadeCount = events.OfType<ChoiceMade>().Count();
//         var roundCompletedCount = events.OfType<RoundCompleted>().Count();
//         var gameEndedCount = events.OfType<GameWon>().Count() + events.OfType<GameDraw>().Count();
//
//         gameStartedCount.ShouldBe(1); // Always has one game started event
//         gameEndedCount.ShouldBe(1); // Always has one game ended event (won or draw)
//         roundCompletedCount.ShouldBeInRange(2, 3); // Could be fewer than totalRounds if won early
//         choiceMadeCount.ShouldBe(roundCompletedCount * 2); // Each round has 2 choice made events
//     }
//
//     [Test]
//     public void returned_game_id_matches_events_game_id() {
//         // Act
//         var (gameId, events, _) = RockPaperScissorsSimulator.SimulateGame();
//
//         // Assert
//         var gameStartedEvent = events.OfType<GameStarted>().Single();
//         gameId.ShouldBe(gameStartedEvent.GameId);
//
//         // All events should have the same GameId
//         foreach (var ev in events) {
//             switch (ev) {
//                 case GameStarted gs:
//                     gs.GameId.ShouldBe(gameId);
//                     break;
//                 case ChoiceMade cm:
//                     cm.GameId.ShouldBe(gameId);
//                     break;
//                 case RoundCompleted rc:
//                     rc.GameId.ShouldBe(gameId);
//                     break;
//                 case GameWon gw:
//                     gw.GameId.ShouldBe(gameId);
//                     break;
//                 case GameDraw gd:
//                     gd.GameId.ShouldBe(gameId);
//                     break;
//             }
//         }
//     }
//
//     [Test]
//     public void returned_winner_matches_last_event_winner_or_null_for_draw() {
//         // Act
//         var (_, events, winner) = RockPaperScissorsSimulator.SimulateGame();
//         var lastEvent = events.Last();
//
//         // Assert
//         if (lastEvent is GameWon gameWon)
//             winner.ShouldBe(gameWon.Winner);
//         else if (lastEvent is GameDraw)
//             winner.ShouldBeNull();
//     }
//
//     [Test]
//     public void simulated_game_events_can_reconstruct_a_consistent_final_game_state() {
//         // Act
//         var (gameId, events, winner) = RockPaperScissorsSimulator.SimulateGame();
//
//         // Arrange - reconstruct the game state from events
//         var reconstructedGame = new Game();
//
//         foreach (var ev in events)
//             (reconstructedGame, _) = ev switch {
//                 GameStarted gs => reconstructedGame.Execute(new StartGame(gs.GameId, gs.TotalRounds)),
//                 ChoiceMade cm => reconstructedGame.Execute(new MakeChoice(cm.GameId, cm.Player, cm.Choice)),
//                 _ => (reconstructedGame, [])
//             };
//
//         // Assert
//         var lastEvent = events.Last();
//         reconstructedGame.Id.ShouldBe(gameId);
//         reconstructedGame.Winner.ShouldBe(winner);
//
//         if (lastEvent is GameWon gameWon) {
//             reconstructedGame.Status.ShouldBeOneOf(GameStatus.Player1Won, GameStatus.Player2Won);
//             reconstructedGame.Winner.ShouldBe(gameWon.Winner);
//             reconstructedGame.Player1Score.ShouldBe(gameWon.Player1Score);
//             reconstructedGame.Player2Score.ShouldBe(gameWon.Player2Score);
//         }
//         else if (lastEvent is GameDraw) {
//             reconstructedGame.Status.ShouldBe(GameStatus.Draw);
//             reconstructedGame.Winner.ShouldBeNull();
//             reconstructedGame.Player1Score.ShouldBe(reconstructedGame.Player2Score);
//         }
//         else
//             true.ShouldBeFalse("Last event was not GameWon or GameDraw"); // Should not happen
//     }
//
//     [Test]
//     public void different_round_counts_work_correctly() {
//         // Test with 1, 3, and 5 rounds
//         foreach (var roundCount in new[] { 1, 3, 5 }) {
//             // Act
//             var (_, events, _) = RockPaperScissorsSimulator.SimulateGame(roundCount);
//
//             // Assert
//             var gameStartedEvent = events.First().ShouldBeOfType<GameStarted>();
//             gameStartedEvent.TotalRounds.ShouldBe(roundCount);
//
//             // Game should be completed
//             (events.Last() is GameWon or GameDraw).ShouldBeTrue();
//         }
//     }
//
//     // Helper method to determine the expected winner based on game rules
//     private static Player? DetermineExpectedWinner(Choice player1Choice, Choice player2Choice) {
//         if (player1Choice == player2Choice)
//             return null; // Draw
//
//         return (player1Choice, player2Choice) switch {
//             (Choice.Rock, Choice.Scissors)     => Player.Player1,
//             (Choice.Paper, Choice.Rock)        => Player.Player1,
//             (Choice.Scissors, Choice.Paper)    => Player.Player1,
//             _                                  => Player.Player2
//         };
//     }
// }
