using System;
using System.Linq;

namespace ChessAR
{
    public enum LessonMode { Setup, Moves }

    public sealed class ChessExample
    {
        readonly Piece[] _position;
        readonly Square[] _destinations;
        public string Title { get; }
        public string Instruction { get; }
        public Piece SelectedPiece { get; }
        public Piece[] Position => (Piece[])_position.Clone();
        public Square[] Destinations => (Square[])_destinations.Clone();

        internal ChessExample(string title, string instruction, Piece selected, params Piece[] others)
        {
            Title = title;
            Instruction = instruction;
            SelectedPiece = selected;
            _position = new[] { selected }.Concat(others).ToArray();
            _destinations = ChessRules.Moves(selected, _position);
        }
    }

    public static class ChessExamples
    {
        static Piece At(PieceKind kind, Side side, string square) => new Piece(kind, side, Square.Parse(square));

        public static ChessExample For(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return new ChessExample("폰의 이동",
                        "앞으로 한 칸, 처음에는 두 칸까지 갑니다.\n상대 말은 대각선 한 칸으로 잡습니다.",
                        At(kind, Side.White, "d2"), At(PieceKind.Pawn, Side.Black, "c3"), At(PieceKind.Pawn, Side.White, "e3"));
                case PieceKind.Rook:
                    return new ChessExample("룩의 이동",
                        "가로·세로로 이동합니다. 같은 편 말 앞에서 멈추고, 상대 말을 잡으면 그 칸에서 멈춥니다.",
                        At(kind, Side.White, "d4"), At(PieceKind.Pawn, Side.White, "d6"), At(PieceKind.Pawn, Side.Black, "f4"));
                case PieceKind.Knight:
                    return new ChessExample("나이트의 이동",
                        "두 칸과 한 칸을 꺾어 L자로 갑니다. 다른 말을 뛰어넘을 수 있지만, 같은 편 말이 있는 칸에는 놓지 않습니다.",
                        At(kind, Side.White, "d4"), At(PieceKind.Pawn, Side.White, "d5"),
                        At(PieceKind.Pawn, Side.White, "f5"), At(PieceKind.Pawn, Side.Black, "e6"));
                case PieceKind.Bishop:
                    return new ChessExample("비숍의 이동",
                        "대각선으로 이동합니다. 다른 말을 뛰어넘을 수 없고, 처음과 같은 색의 칸으로만 갑니다.",
                        At(kind, Side.White, "d4"), At(PieceKind.Pawn, Side.White, "f6"), At(PieceKind.Pawn, Side.Black, "b6"));
                case PieceKind.Queen:
                    return new ChessExample("퀸의 이동",
                        "가로·세로·대각선으로 이동합니다.\n다른 말을 뛰어넘을 수 없습니다.",
                        At(kind, Side.White, "d4"), At(PieceKind.Pawn, Side.White, "d6"), At(PieceKind.Pawn, Side.Black, "f4"));
                case PieceKind.King:
                    return new ChessExample("킹의 이동",
                        "주변 한 칸으로 이동합니다. 실제 경기에서는 공격받는 칸으로 갈 수 없습니다. 여기서는 기본 방향만 표시합니다.",
                        At(kind, Side.White, "d4"), At(PieceKind.Pawn, Side.White, "c3"), At(PieceKind.Pawn, Side.Black, "e4"));
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    public sealed class ChessLessonState
    {
        static readonly string[] SetupTitles =
        {
            "판 방향", "흰 폰 놓기", "흰 말의 뒷줄", "검은 폰 놓기", "검은 말의 뒷줄"
        };
        static readonly string[] SetupInstructions =
        {
            "흰 말 쪽에서 오른쪽 아래 h1이 밝은 칸이 되도록 놓으세요.",
            "두 번째 줄 a2부터 h2까지 흰 폰 여덟 개를 놓으세요.",
            "a1부터 룩·나이트·비숍·퀸·킹·비숍·나이트·룩 순서입니다. 퀸은 밝은 d1, 킹은 e1에 놓으세요.",
            "일곱 번째 줄 a7부터 h7까지 검은 폰 여덟 개를 놓으세요.",
            "a8부터 룩·나이트·비숍·퀸·킹·비숍·나이트·룩 순서입니다. 퀸은 어두운 d8, 킹은 e8에 놓으세요."
        };
        ChessExample _example = ChessExamples.For(PieceKind.Pawn);

        public LessonMode Mode { get; private set; } = LessonMode.Setup;
        public int SetupStep { get; private set; }
        public int SetupStepCount => SetupTitles.Length;
        public PieceKind SelectedKind { get; private set; } = PieceKind.Pawn;
        public ChessExample CurrentExample => _example;
        public string Title => Mode == LessonMode.Setup ? SetupTitles[SetupStep] : _example.Title;
        public string Instruction => Mode == LessonMode.Setup ? SetupInstructions[SetupStep] : _example.Instruction;
        public bool CanPrevious => Mode == LessonMode.Setup && SetupStep > 0;
        public bool CanNext => Mode == LessonMode.Setup && SetupStep < SetupStepCount - 1;

        public Piece[] DisplayedPieces => Mode == LessonMode.Moves ? _example.Position :
            ChessRules.InitialPosition().Where(piece =>
                (piece.Side == Side.White && piece.Kind == PieceKind.Pawn && SetupStep >= 1) ||
                (piece.Side == Side.White && piece.Kind != PieceKind.Pawn && SetupStep >= 2) ||
                (piece.Side == Side.Black && piece.Kind == PieceKind.Pawn && SetupStep >= 3) ||
                (piece.Side == Side.Black && piece.Kind != PieceKind.Pawn && SetupStep >= 4)).ToArray();

        public bool Next()
        {
            if (!CanNext) return false;
            SetupStep++;
            return true;
        }

        public bool Previous()
        {
            if (!CanPrevious) return false;
            SetupStep--;
            return true;
        }

        public void ShowSetup() => Mode = LessonMode.Setup;

        public void SelectKind(PieceKind kind)
        {
            var example = ChessExamples.For(kind);
            SelectedKind = kind;
            _example = example;
            Mode = LessonMode.Moves;
        }

        public void Restart()
        {
            SetupStep = 0;
            Mode = LessonMode.Setup;
            SelectedKind = PieceKind.Pawn;
            _example = ChessExamples.For(SelectedKind);
        }
    }
}
