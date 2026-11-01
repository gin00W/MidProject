# Scan bridge regression checks

From the repository root, with .NET SDK 8:

```sh
dotnet run --project Tests/ScanRegression/ScanRegression.csproj --configuration Release
```

These dependency-free console checks compile the production native payload
validator, request lifecycle state, RGBA orientation helper and camera crop geometry. JSON examples are
test fixtures; no test simulates a successful camera recognition in the app.

They cover malformed confidence/labels, request cancellation and re-request,
stale callbacks, the 32-label envelope boundary, four rear-camera EXIF pixel
transformations, and visible centered crops on tall phones and iPads. They do not
compile Apple frameworks or execute ARKit, Unity JsonUtility, camera capture,
Vision, Xcode builds or a real iPhone. Those require device validation.
