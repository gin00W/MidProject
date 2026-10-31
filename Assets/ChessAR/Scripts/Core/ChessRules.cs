using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessAR
{
    public enum PieceKind { Pawn, Rook, Knight, Bishop, Queen, King }
    public enum Side { White, Black }

    public readonly struct Square : IEquatable<Square>
    {
        public int File { get; }
        public int Rank { get; }
        public string Name => ((char)('a' + File)).ToString() + (Rank + 1);

        public Square(int file, int rank)
        {
            if (!IsInside(file, rank)) throw new ArgumentOutOfRangeException(nameof(file), "A square must lie inside a1–h8.");
            File = file;
            Rank = rank;
        }

        public static bool IsInside(int file, int rank) => file >= 0 && file < 8 && rank >= 0 && rank < 8;

        public static Square Parse(string name)
        {
            if (name == null || name.Length != 2 || name[0] < 'a' || name[0] > 'h' || name[1] < '1' || name[1] > '8')
                throw new ArgumentException("Use a square name from a1 through h8.", nameof(name));
            return new Square(name[0] - 'a', name[1] - '1');
        }

        public bool Equals(Square other) => File == other.File && Rank == other.Rank;
        public override bool Equals(object obj) => obj is Square other && Equals(other);
        public override int GetHashCode() => File + Rank * 8;
        public override string ToString() => Name;
        public static bool operator ==(Square left, Square right) => left.Equals(right);
        public static bool operator !=(Square left, Square right) => !left.Equals(right);
    }

    public sealed class Piece
    {
        public PieceKind Kind { get; }
        public Side Side { get; }
        public Square Square { get; }

        public Piece(PieceKind kind, Side side, Square square)
        {
            if (!Enum.IsDefined(typeof(PieceKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(Side), side)) throw new ArgumentOutOfRangeException(nameof(side));
            Kind = kind;
            Side = side;
            Square = square;
        }
    }

    // These are occupancy-aware movement patterns for a teaching position.
    // They are NOT a match engine: check, king safety, castling, en passant,
    // promotion and game outcomes are deliberately outside this API.
    public static class ChessRules
    {
        public const string LearningScopeNotice = "기본 이동 예시입니다. 체크·캐슬링·앙파상·승격은 다루지 않습니다.";
        static readonly PieceKind[] BackRank =
        {
            PieceKind.Rook, PieceKind.Knight, PieceKind.Bishop, PieceKind.Queen,
            PieceKind.King, PieceKind.Bishop, PieceKind.Knight, PieceKind.Rook
        };

        public static Piece[] InitialPosition()
        {
            var pieces = new List<Piece>(32);
            for (int file = 0; file < 8; file++)
            {
                pieces.Add(new Piece(BackRank[file], Side.White, new Square(file, 0)));
                pieces.Add(new Piece(PieceKind.Pawn, Side.White, new Square(file, 1)));
                pieces.Add(new Piece(PieceKind.Pawn, Side.Black, new Square(file, 6)));
                pieces.Add(new Piece(BackRank[file], Side.Black, new Square(file, 7)));
            }
            return pieces.ToArray();
        }

        public static bool IsDark(Square square) => (square.File + square.Rank) % 2 == 0;

        public static Square[] Moves(Piece selected, IEnumerable<Piece> position)
        {
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            var occupied = new Dictionary<Square, Piece>();
            foreach (var piece in position ?? Array.Empty<Piece>())
            {
                if (piece == null) throw new ArgumentException("A position cannot contain a null piece.", nameof(position));
                if (occupied.ContainsKey(piece.Square))
                    throw new ArgumentException("Two pieces occupy " + piece.Square.Name + ".", nameof(position));
                occupied.Add(piece.Square, piece);
            }
            if (occupied.TryGetValue(selected.Square, out var source) &&
                (source.Kind != selected.Kind || source.Side != selected.Side))
                throw new ArgumentException("The selected piece disagrees with its occupied source square.", nameof(selected));

            var result = new List<Square>();
            switch (selected.Kind)
            {
                case PieceKind.Pawn:
                    int direction = selected.Side == Side.White ? 1 : -1;
                    int firstRank = selected.Side == Side.White ? 1 : 6;
                    int one = selected.Square.Rank + direction;
                    if (Square.IsInside(selected.Square.File, one) &&
                        !occupied.ContainsKey(new Square(selected.Square.File, one)))
                    {
                        result.Add(new Square(selected.Square.File, one));
                        int two = one + direction;
                        if (selected.Square.Rank == firstRank &&
                            !occupied.ContainsKey(new Square(selected.Square.File, two)))
                            result.Add(new Square(selected.Square.File, two));
                    }
                    foreach (int offset in new[] { -1, 1 })
                    {
                        int file = selected.Square.File + offset;
                        if (!Square.IsInside(file, one)) continue;
                        var square = new Square(file, one);
                        if (occupied.TryGetValue(square, out var target) && IsCapturable(selected, target))
                            result.Add(square);
                    }
                    break;
                case PieceKind.Knight:
                    foreach (var offset in new[] { (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2) })
                        AddLanding(selected, occupied, result, offset.Item1, offset.Item2);
                    break;
                case PieceKind.King:
                    for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        if (x != 0 || y != 0) AddLanding(selected, occupied, result, x, y);
                    break;
                case PieceKind.Rook:
                    AddStraight(selected, occupied, result);
                    break;
                case PieceKind.Bishop:
                    AddDiagonal(selected, occupied, result);
                    break;
                case PieceKind.Queen:
                    AddStraight(selected, occupied, result);
                    AddDiagonal(selected, occupied, result);
                    break;
            }
            return result.OrderBy(square => square.Rank).ThenBy(square => square.File).ToArray();
        }

        // A king is checked, not captured. Safety of the moving side's king is
        // still not calculated by this basic-movement teaching helper.
        static bool IsCapturable(Piece selected, Piece target) =>
            target.Side != selected.Side && target.Kind != PieceKind.King;

        static void AddLanding(Piece selected, Dictionary<Square, Piece> occupied,
            List<Square> result, int dx, int dy)
        {
            int file = selected.Square.File + dx;
            int rank = selected.Square.Rank + dy;
            if (!Square.IsInside(file, rank)) return;
            var square = new Square(file, rank);
            if (!occupied.TryGetValue(square, out var target) || IsCapturable(selected, target))
                result.Add(square);
        }

        static void AddStraight(Piece piece, Dictionary<Square, Piece> occupied, List<Square> result)
        {
            Ray(piece, occupied, result, 1, 0); Ray(piece, occupied, result, -1, 0);
            Ray(piece, occupied, result, 0, 1); Ray(piece, occupied, result, 0, -1);
        }

        static void AddDiagonal(Piece piece, Dictionary<Square, Piece> occupied, List<Square> result)
        {
            Ray(piece, occupied, result, 1, 1); Ray(piece, occupied, result, -1, 1);
            Ray(piece, occupied, result, 1, -1); Ray(piece, occupied, result, -1, -1);
        }

        static void Ray(Piece piece, Dictionary<Square, Piece> occupied, List<Square> result, int dx, int dy)
        {
            for (int distance = 1; distance < 8; distance++)
            {
                int file = piece.Square.File + dx * distance;
                int rank = piece.Square.Rank + dy * distance;
                if (!Square.IsInside(file, rank)) break;
                var square = new Square(file, rank);
                if (occupied.TryGetValue(square, out var target))
                {
                    if (IsCapturable(piece, target)) result.Add(square);
                    break;
                }
                result.Add(square);
            }
        }
    }

    public readonly struct BoardPoint
    {
        public float X { get; }
        public float Z { get; }
        public BoardPoint(float x, float z) { X = x; Z = z; }
    }

    public static class BoardLayout
    {
        public const float TargetWidth = .21f;
        public const float TargetHeight = .297f;
        public const float GridWidth = .176f;
        public const float CellWidth = GridWidth / 8;

        // The A4 reference image and grid share a centre. +X is toward h-file,
        // +Z toward rank 8; rendering applies the image pose outside this helper.
        public static BoardPoint ToLocal(Square square) =>
            new BoardPoint((square.File - 3.5f) * CellWidth, (square.Rank - 3.5f) * CellWidth);
    }
}
