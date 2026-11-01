# Chess learning regression checks

Run from the repository root with .NET SDK 8:

```sh
dotnet run --project Tests/ChessRegression --configuration Release
```

The console harness compiles the actual dependency-free chess core. It checks
the 32-piece initial position, square colours and physical board coordinates,
occupancy-aware pawn/ray/knight/king movement, capture and blocker behaviour,
all-square bounds and rotation symmetry, authored teaching examples, and setup
state transitions.

Movement results are **pseudolegal teaching patterns**, not legal moves in a
complete match. King safety/check, castling, en passant, promotion and game
outcomes are excluded; an enemy king is treated as a non-capturable blocker.
The UI must state this learning scope. These tests do not run Unity, ARKit image
tracking, 3D rendering, print scaling or an iOS build.
