// ReSharper disable CheckNamespace
namespace TicTacToe;

public enum Player {
    X, O
}

public enum GameStatus {
    Ongoing,
    Won,
    Draw
}

public record Position(int Row, int Column);

public record Cell(Position Position, CellState State);

public record Board(Cell[] Cells) {
    public static Board CreateEmpty() {
        var cells = Enumerable.Range(0, 3)
            .SelectMany(row => Enumerable.Range(0, 3).Select(col => new Cell(new Position(row, col), CellState.Empty)))
            .ToArray();

        return new(cells);
    }
}

public enum CellState {
    Empty,
    X,
    O
}

// Commands
public record StartGame(Guid GameId, Player StartingPlayer);

public record MakeMove(Guid GameId, Position Position, Player Player);

// Events
public record GameStarted(Guid GameId, Player StartingPlayer);

public record MoveMade(Guid GameId, Position Position, Player Player);

public record GameWon(Guid GameId, Player Winner, Position[] WinningLine);

public record GameDraw(Guid GameId);

public record Game {
    public Guid       Id            { get; init; }
    public Board      Board         { get; init; } = Board.CreateEmpty();
    public Player     CurrentPlayer { get; init; }
    public GameStatus Status        { get; init; } = GameStatus.Ongoing;
    public Player?    Winner        { get; init; }

    public (Game UpdatedGame, object[] Events) Execute(object command) =>
        command switch {
            StartGame cmd                                  => StartNewGame(cmd),
            MakeMove cmd when Status == GameStatus.Ongoing => MakePlayerMove(cmd),
            _                                              => throw new InvalidOperationException("Invalid command")
        };

    (Game, object[]) StartNewGame(StartGame cmd) => (this with { Id = cmd.GameId, CurrentPlayer = cmd.StartingPlayer }, [new GameStarted(cmd.GameId, cmd.StartingPlayer)]);

    (Game, object[]) MakePlayerMove(MakeMove cmd) {
        if (cmd.Player != CurrentPlayer || Board.Cells
                .First(c => c.Position == cmd.Position).State != CellState.Empty)
            throw new InvalidOperationException("Invalid move");

        var newBoard = new Board(Board.Cells.Select(c => c.Position == cmd.Position ? c with { State = (CellState)cmd.Player } : c).ToArray());

        var events  = new List<object> { new MoveMade(Id, cmd.Position, cmd.Player) };
        var newGame = this with { Board = newBoard };

        if (CheckWin(newBoard, cmd.Player, out var winningLine)) {
            events.Add(new GameWon(Id, cmd.Player, winningLine));
            return (newGame with { Status = GameStatus.Won, Winner = cmd.Player }, events.ToArray());
        }

        if (newBoard.Cells.All(c => c.State != CellState.Empty)) {
            events.Add(new GameDraw(Id));
            return (newGame with { Status = GameStatus.Draw }, events.ToArray());
        }

        return (newGame with { CurrentPlayer = cmd.Player == Player.X ? Player.O : Player.X }, events.ToArray());
    }

    static bool CheckWin(Board board, Player player, out Position[] winningLine) {
        var playerState = (CellState)player;

        // Check rows
        for (var row = 0; row < 3; row++) {
            var rowCells = board.Cells.Where(c => c.Position.Row == row).ToArray();
            if (rowCells.All(c => c.State == playerState)) {
                winningLine = rowCells.Select(c => c.Position).ToArray();
                return true;
            }
        }

        // Check columns
        for (var col = 0; col < 3; col++) {
            var colCells = board.Cells.Where(c => c.Position.Column == col).ToArray();
            if (colCells.All(c => c.State == playerState)) {
                winningLine = colCells.Select(c => c.Position).ToArray();
                return true;
            }
        }

        // Check main diagonal
        var mainDiag = board.Cells.Where(c => c.Position.Row == c.Position.Column).ToArray();
        if (mainDiag.All(c => c.State == playerState)) {
            winningLine = mainDiag.Select(c => c.Position).ToArray();
            return true;
        }

        // Check anti-diagonal
        var antiDiag = board.Cells.Where(c => c.Position.Row + c.Position.Column == 2).ToArray();
        if (antiDiag.All(c => c.State == playerState)) {
            winningLine = antiDiag.Select(c => c.Position).ToArray();
            return true;
        }

        winningLine = [];
        return false;
    }
}
