using System;
using System.Collections.Generic;
using System.Linq;
using ChessAR;

static class Program
{
    static int _passed;
    static int _failed;

    static int Main()
    {
        Run("square names cover a1 through h8 and retain value equality", () =>
        {
            Check(new Square(0, 0).Name == "a1" && new Square(7, 7).Name == "h8", "Corner labels reversed.");
            Check(Square.Parse("d4") == new Square(3, 3), "Square parse changed coordinates.");
            var set = new HashSet<Square> { Square.Parse("d4"), new Square(3, 3) };
            Check(set.Count == 1, "Equal squares have different hashes.");
        });
        Run("off-board and malformed square inputs are rejected", () =>
        {
            foreach (var value in new[] { (-1, 0), (8, 0), (0, -1), (0, 8) })
                Reject(() => new Square(value.Item1, value.Item2));
            foreach (var value in new[] { "", "A1", "a0", "i2", "a10", "aa", null })
                Reject(() => Square.Parse(value));
        });
        Run("unknown piece kinds and sides are rejected", () =>
        {
            Reject(() => new Piece((PieceKind)99, Side.White, Square.Parse("a1")));
            Reject(() => new Piece(PieceKind.Rook, (Side)99, Square.Parse("a1")));
        });
        Run("a1 is dark and h1 is light with alternating neighbours", () =>
        {
            Check(ChessRules.IsDark(Square.Parse("a1")), "a1 must be dark.");
            Check(!ChessRules.IsDark(Square.Parse("h1")), "Right-hand corner must be light.");
            for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 7; file++)
                Check(ChessRules.IsDark(new Square(file, rank)) != ChessRules.IsDark(new Square(file + 1, rank)),
                    "Adjacent squares share a colour.");
        });
        Run("initial position has 32 unique squares and 16 pieces per side", () =>
        {
            var pieces = ChessRules.InitialPosition();
            Check(pieces.Length == 32 && pieces.Select(p => p.Square).Distinct().Count() == 32, "Duplicate initial squares.");
            foreach (Side side in Enum.GetValues(typeof(Side)))
            {
                Check(pieces.Count(p => p.Side == side) == 16, "Side does not have 16 pieces.");
                Check(pieces.Count(p => p.Side == side && p.Kind == PieceKind.Pawn) == 8, "Pawn count wrong.");
                Check(pieces.Count(p => p.Side == side && p.Kind == PieceKind.Rook) == 2, "Rook count wrong.");
                Check(pieces.Count(p => p.Side == side && p.Kind == PieceKind.Knight) == 2, "Knight count wrong.");
                Check(pieces.Count(p => p.Side == side && p.Kind == PieceKind.Bishop) == 2, "Bishop count wrong.");
                Check(pieces.Count(p => p.Side == side && p.Kind == PieceKind.Queen) == 1, "Queen count wrong.");
                Check(pieces.Count(p => p.Side == side && p.Kind == PieceKind.King) == 1, "King count wrong.");
            }
        });
        Run("both back ranks have queen on d-file and king on e-file", () =>
        {
            var pieces = ChessRules.InitialPosition();
            AssertPiece(pieces, "d1", PieceKind.Queen, Side.White);
            AssertPiece(pieces, "e1", PieceKind.King, Side.White);
            AssertPiece(pieces, "d8", PieceKind.Queen, Side.Black);
            AssertPiece(pieces, "e8", PieceKind.King, Side.Black);
            AssertPiece(pieces, "a1", PieceKind.Rook, Side.White);
            AssertPiece(pieces, "h8", PieceKind.Rook, Side.Black);
            AssertPiece(pieces, "c1", PieceKind.Bishop, Side.White);
            AssertPiece(pieces, "f8", PieceKind.Bishop, Side.Black);
            Check(!ChessRules.IsDark(Square.Parse("d1")) && ChessRules.IsDark(Square.Parse("d8")),
                "Queens do not stand on their own colours.");
        });
        Run("initial position callers cannot mutate a later initial position", () =>
        {
            var pieces = ChessRules.InitialPosition();
            pieces[0] = null;
            Check(ChessRules.InitialPosition().All(p => p != null), "Initial position reused a mutable array.");
        });
        Run("white pawn advances one or two empty squares from its starting rank", () =>
            Destinations(At(PieceKind.Pawn, Side.White, "d2"), null, "d3 d4"));
        Run("black pawn advances in the opposite direction", () =>
            Destinations(At(PieceKind.Pawn, Side.Black, "e7"), null, "e6 e5"));
        Run("pawn outside starting rank cannot move two squares", () =>
        {
            Destinations(At(PieceKind.Pawn, Side.White, "d3"), null, "d4");
            Destinations(At(PieceKind.Pawn, Side.Black, "e6"), null, "e5");
        });
        Run("pawn cannot jump a blocked first square or capture straight ahead", () =>
        {
            var pawn = At(PieceKind.Pawn, Side.White, "d2");
            Destinations(pawn, new[] { pawn, At(PieceKind.Rook, Side.Black, "d3") }, "");
            Destinations(pawn, new[] { pawn, At(PieceKind.Pawn, Side.White, "d3") }, "");
        });
        Run("blocked second pawn square still allows one step", () =>
        {
            var pawn = At(PieceKind.Pawn, Side.White, "d2");
            Destinations(pawn, new[] { pawn, At(PieceKind.Rook, Side.Black, "d4") }, "d3");
        });
        Run("pawn captures enemies diagonally but not friendly pieces", () =>
        {
            var pawn = At(PieceKind.Pawn, Side.White, "d2");
            Destinations(pawn, new[] { pawn, At(PieceKind.Rook, Side.Black, "c3"),
                At(PieceKind.Pawn, Side.White, "e3") }, "c3 d3 d4");
        });
        Run("black pawn diagonal captures are mirrored", () =>
        {
            var pawn = At(PieceKind.Pawn, Side.Black, "e7");
            Destinations(pawn, new[] { pawn, At(PieceKind.Rook, Side.White, "d6"),
                At(PieceKind.Pawn, Side.Black, "f6") }, "d6 e6 e5");
        });
        Run("edge pawns cannot wrap across files or move past the board", () =>
        {
            var pawn = At(PieceKind.Pawn, Side.White, "a2");
            Destinations(pawn, new[] { pawn, At(PieceKind.Pawn, Side.Black, "b3"),
                At(PieceKind.Pawn, Side.Black, "h2") }, "a3 a4 b3");
            Destinations(At(PieceKind.Pawn, Side.White, "a8"), null, "");
            Destinations(At(PieceKind.Pawn, Side.Black, "h1"), null, "");
        });
        Run("rook stops at a friendly blocker and at its first capture", () =>
            ExampleDestinations(PieceKind.Rook, "a4 b4 c4 d1 d2 d3 d5 e4 f4"));
        Run("bishop stays diagonal and cannot pass through occupied squares", () =>
            ExampleDestinations(PieceKind.Bishop, "a1 b2 c3 e3 f2 g1 c5 b6 e5"));
        Run("queen combines straight and diagonal rays without duplicate destinations", () =>
            ExampleDestinations(PieceKind.Queen, "a4 b4 c4 d1 d2 d3 d5 e4 f4 e5 f6 g7 h8 c5 b6 a7 e3 f2 g1 c3 b2 a1"));
        Run("knight jumps over pieces but cannot land on a friendly piece", () =>
            ExampleDestinations(PieceKind.Knight, "b3 b5 c2 c6 e2 e6 f3"));
        Run("knight and king at a corner have no off-board destinations", () =>
        {
            Destinations(At(PieceKind.Knight, Side.White, "a1"), null, "b3 c2");
            Destinations(At(PieceKind.King, Side.White, "a1"), null, "a2 b1 b2");
        });
        Run("king moves one square and cannot land on a friendly piece", () =>
            ExampleDestinations(PieceKind.King, "c4 c5 d3 d5 e3 e4 e5"));
        Run("a king is never presented as a capturable piece", () =>
        {
            var rook = At(PieceKind.Rook, Side.White, "d4");
            var moves = ChessRules.Moves(rook, new[] { rook, At(PieceKind.King, Side.Black, "d6") });
            Check(moves.Contains(Square.Parse("d5")) && !moves.Contains(Square.Parse("d6")) &&
                !moves.Contains(Square.Parse("d7")), "Enemy king was captured or passed through.");
        });
        Run("basic king movement is explicitly not a check-safety solver", () =>
        {
            var king = At(PieceKind.King, Side.White, "d4");
            var moves = ChessRules.Moves(king, new[] { king, At(PieceKind.Rook, Side.Black, "e8") });
            Check(moves.Contains(Square.Parse("e4")), "Learning helper silently claimed full king legality.");
            Check(ChessRules.LearningScopeNotice.Contains("체크") && ChessExamples.For(PieceKind.King).Instruction.Contains("공격받는"),
                "King-safety limitation is missing from authored learning text.");
        });
        Run("initial rook bishop and king are blocked while the knight can jump", () =>
        {
            var pieces = ChessRules.InitialPosition();
            foreach (string square in new[] { "a1", "c1", "e1" })
                Check(ChessRules.Moves(pieces.Single(p => p.Square == Square.Parse(square)), pieces).Length == 0,
                    "An initial blocked piece escaped from " + square);
            Destinations(pieces.Single(p => p.Square == Square.Parse("b1")), pieces, "a3 c3");
        });
        Run("every piece on every square yields unique in-bounds destinations excluding its source", () =>
        {
            foreach (PieceKind kind in Enum.GetValues(typeof(PieceKind)))
            foreach (Side side in Enum.GetValues(typeof(Side)))
            for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                var selected = new Piece(kind, side, new Square(file, rank));
                var moves = ChessRules.Moves(selected, new[] { selected });
                Check(moves.Distinct().Count() == moves.Length && !moves.Contains(selected.Square), "Duplicate/source destination.");
                Check(moves.All(s => Square.IsInside(s.File, s.Rank)), "Off-board destination.");
            }
        });
        Run("empty-board movement is symmetric after rotating board and swapping sides", () =>
        {
            foreach (PieceKind kind in Enum.GetValues(typeof(PieceKind)))
            for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                var white = new Piece(kind, Side.White, new Square(file, rank));
                var black = new Piece(kind, Side.Black, new Square(7 - file, 7 - rank));
                var expected = ChessRules.Moves(white, null).Select(s => new Square(7 - s.File, 7 - s.Rank));
                Check(new HashSet<Square>(expected).SetEquals(ChessRules.Moves(black, null)), "Rotation asymmetry for " + kind);
            }
        });
        Run("invalid positions cannot silently overwrite duplicated or inconsistent occupants", () =>
        {
            var rook = At(PieceKind.Rook, Side.White, "d4");
            Reject(() => ChessRules.Moves(null, null));
            Reject(() => ChessRules.Moves(rook, new[] { rook, rook }));
            Reject(() => ChessRules.Moves(rook, new Piece[] { null }));
            Reject(() => ChessRules.Moves(rook, new[] { At(PieceKind.Bishop, Side.Black, "d4") }));
        });
        Run("board geometry fits the registered A4 page and centres all 64 squares", () =>
        {
            Near(BoardLayout.TargetWidth, .21f); Near(BoardLayout.TargetHeight, .297f);
            Near(BoardLayout.GridWidth, .176f); Near(BoardLayout.CellWidth, .022f);
            var a1 = BoardLayout.ToLocal(Square.Parse("a1"));
            var h8 = BoardLayout.ToLocal(Square.Parse("h8"));
            Near(a1.X, -.077f); Near(a1.Z, -.077f); Near(h8.X, .077f); Near(h8.Z, .077f);
            Near(a1.X + h8.X, 0); Near(a1.Z + h8.Z, 0);
            Check(BoardLayout.GridWidth < BoardLayout.TargetWidth && BoardLayout.GridWidth < BoardLayout.TargetHeight,
                "Grid exceeds marker page.");
            for (int file = 0; file < 7; file++)
                Near(BoardLayout.ToLocal(new Square(file + 1, 0)).X - BoardLayout.ToLocal(new Square(file, 0)).X,
                    BoardLayout.CellWidth);
        });
        Run("setup begins with orientation only and adds eight pieces per step", () =>
        {
            var state = new ChessLessonState();
            Check(state.Mode == LessonMode.Setup && state.SetupStep == 0 && state.DisplayedPieces.Length == 0,
                "Setup did not begin with an empty orientation board.");
            for (int step = 1; step <= 4; step++)
            {
                Check(state.Next(), "Next setup step failed.");
                Check(state.SetupStep == step && state.DisplayedPieces.Length == step * 8, "Cumulative setup count wrong.");
            }
            Check(!state.Next() && !state.CanNext && state.DisplayedPieces.Length == 32, "Setup exceeded final step.");
        });
        Run("setup stage groups match the printed white and black sides", () =>
        {
            var state = new ChessLessonState();
            state.Next();
            Check(state.DisplayedPieces.All(p => p.Side == Side.White && p.Kind == PieceKind.Pawn && p.Square.Rank == 1),
                "First group must be white pawns on rank2.");
            state.Next();
            Check(state.DisplayedPieces.All(p => p.Side == Side.White), "White setup contains black pieces.");
            state.Next();
            Check(state.DisplayedPieces.Where(p => p.Side == Side.Black).All(p => p.Kind == PieceKind.Pawn && p.Square.Rank == 6),
                "Third group must be black pawns on rank7.");
        });
        Run("previous cannot underflow setup and removes each cumulative group", () =>
        {
            var state = new ChessLessonState();
            Check(!state.Previous() && !state.CanPrevious, "Initial step underflowed.");
            for (int i = 0; i < 4; i++) state.Next();
            for (int i = 3; i >= 0; i--)
            {
                Check(state.Previous() && state.DisplayedPieces.Length == i * 8, "Previous group did not disappear.");
            }
            Check(!state.Previous(), "Negative setup step accepted.");
        });
        Run("movement mode preserves setup progress and returning setup resumes it", () =>
        {
            var state = new ChessLessonState();
            state.Next(); state.Next();
            state.SelectKind(PieceKind.Knight);
            Check(state.Mode == LessonMode.Moves && state.SelectedKind == PieceKind.Knight &&
                state.CurrentExample.SelectedPiece.Kind == PieceKind.Knight, "Move selection failed.");
            Check(!state.Next() && !state.Previous(), "Movement mode changed hidden setup progress.");
            state.ShowSetup();
            Check(state.Mode == LessonMode.Setup && state.SetupStep == 2 && state.DisplayedPieces.Length == 16,
                "Returning to setup lost progress.");
        });
        Run("restart returns to orientation and invalid selections do not mutate state", () =>
        {
            var state = new ChessLessonState();
            state.Next(); state.SelectKind(PieceKind.Queen);
            Reject(() => state.SelectKind((PieceKind)99));
            Check(state.SelectedKind == PieceKind.Queen && state.Mode == LessonMode.Moves, "Invalid selection mutated state.");
            state.Restart();
            Check(state.Mode == LessonMode.Setup && state.SetupStep == 0 && state.SelectedKind == PieceKind.Pawn &&
                state.DisplayedPieces.Length == 0, "Restart did not reset the lesson.");
        });
        Run("each piece has a unique teaching example with a valid selected source", () =>
        {
            var titles = new HashSet<string>();
            foreach (PieceKind kind in Enum.GetValues(typeof(PieceKind)))
            {
                var example = ChessExamples.For(kind);
                Check(example.SelectedPiece.Kind == kind && example.Position.Count(p => p.Square == example.SelectedPiece.Square) == 1,
                    "Example selection has no unique source.");
                Check(titles.Add(example.Title) && !string.IsNullOrWhiteSpace(example.Instruction), "Example explanation absent/repeated.");
                Check(example.Destinations.Length > 0 && example.Destinations.Distinct().Count() == example.Destinations.Length,
                    "Example has no useful or unique destinations.");
            }
        });
        Run("example and setup arrays are safe snapshots", () =>
        {
            var example = ChessExamples.For(PieceKind.Rook);
            example.Position[0] = null;
            example.Destinations[0] = Square.Parse("h8");
            Check(example.Position.All(p => p != null) && !example.Destinations.Contains(Square.Parse("h8")),
                "Example internal state was mutated through a returned array.");
            var state = new ChessLessonState(); state.Next();
            state.DisplayedPieces[0] = null;
            Check(state.DisplayedPieces.All(p => p != null), "Setup internal state was mutated.");
        });
        Console.WriteLine("Passed " + _passed + " chess learning checks; failed " + _failed + ".");
        return _failed == 0 ? 0 : 1;
    }

    static Piece At(PieceKind kind, Side side, string name) => new Piece(kind, side, Square.Parse(name));
    static void ExampleDestinations(PieceKind kind, string expected)
    {
        var example = ChessExamples.For(kind);
        Destinations(example.SelectedPiece, example.Position, expected);
        Check(new HashSet<Square>(example.Destinations).SetEquals(ChessRules.Moves(example.SelectedPiece, example.Position)),
            "Authored example destinations differ from the checked movement pattern.");
    }
    static void Destinations(Piece selected, IEnumerable<Piece> pieces, string expected)
    {
        var moves = ChessRules.Moves(selected, pieces);
        var actualNames = new HashSet<string>(moves.Select(s => s.Name));
        var expectedNames = new HashSet<string>(expected.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        Check(actualNames.SetEquals(expectedNames), selected.Kind + " from " + selected.Square.Name + " expected [" +
            expected + "] but got [" + string.Join(" ", actualNames.OrderBy(s => s)) + "].");
    }
    static void AssertPiece(Piece[] pieces, string square, PieceKind kind, Side side)
    {
        var piece = pieces.Single(p => p.Square == Square.Parse(square));
        Check(piece.Kind == kind && piece.Side == side, "Incorrect piece at " + square);
    }
    static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid input accepted.");
    }
    static void Near(float actual, float expected) => Check(Math.Abs(actual - expected) < .000001f,
        "Expected " + expected + ", got " + actual);
    static void Run(string name, Action test)
    {
        try { test(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { _failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
