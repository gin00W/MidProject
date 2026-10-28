# Scan bridge regression checks

From the repository root, with .NET SDK 8:

```sh
dotnet run --project Tests/ScanRegression/ScanRegression.csproj --configuration Release
```

These dependency-free console checks compile the production native payload
validator, request lifecycle state and RGBA orientation helper. JSON examples are
test fixtures; no test simulates a successful camera recognition in the app.

They cover malformed confidence/labels, request cancellation and re-request,
stale callbacks, and four rear-camera EXIF pixel transformations. They do not
compile Apple frameworks or execute ARKit, Unity JsonUtility, camera capture,
Vision, Xcode builds or a real iPhone. Those require device validation.
