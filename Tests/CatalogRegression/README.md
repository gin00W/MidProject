# Catalog regression checks

From the project root, with .NET SDK 8 installed:

```sh
dotnet run --project Tests/CatalogRegression/CatalogRegression.csproj --configuration Release
```

These dependency-free console checks compile the production `WasteCatalog.cs`
and `WasteRecognition.cs`. They cover the expanded everyday catalog, aliases,
complete answer paths, malformed nodes,
and question nodes whose absent `result` field has become an empty object.
That last condition caused the `pet_bottle: 질문 또는 결과 중 하나가 필요합니다.`
exception reported in the Unity preview.

Recognition checks use deterministic synthetic classifier labels as unit-test
inputs. They cover named can suggestions, PET/glass ambiguity, concrete keyword
extensions, weak/unknown labels, can/candle/can-opener false positives, duplicate
labels, invalid scores, and candidate limits. Production recognition still needs
the native iPhone camera classifier; these tests do not fabricate an app scan.

The tests deliberately materialize empty nested result objects to exercise the
production validator. They use System.Text.Json to read the fixtures, so they do
not claim to test Unity's JsonUtility, the preview UI, ARKit, or iPhone execution.
Those still need to be checked in the Unity Editor and on the target device.

This test project lives outside `Assets` and is not imported into the Unity app.
