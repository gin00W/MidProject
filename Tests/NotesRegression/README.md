# Personal note regressions

Run with .NET 8:

```sh
dotnet run --project Tests/NotesRegression/NotesRegression.csproj --configuration Release
```

The tests link the production pure-C# note model and file transaction. They cover
editing/search, copy isolation, field limits, HTTPS references, real temporary-file
save/reload, previous-version backups, interrupted replacement recovery, and
read/write failures preserving existing records. Serialization in this harness is
System.Text.Json; the Unity adapter uses JsonUtility and must also be checked in
Unity. These tests do not run the Unity form or iPhone application.
