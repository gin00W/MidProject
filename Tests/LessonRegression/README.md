# Image AR lesson state checks

With .NET SDK 8, from the repository root:

```sh
dotnet run --project Tests/LessonRegression/LessonRegression.csproj --configuration Release
```

This dependency-free harness compiles the production `WasteLessonState` and
checks progression, bounds, restart, clear, switching registered images and
preserving progress when tracking continuously reports or reacquires the same
image. It also checks that supported lesson IDs exist in the real guide catalog
and have corresponding image target files.

The PET example concerns transparent water/beverage bottles; the container
example asks the learner to assess contamination. Advancing a lesson does not
classify real material, prove cleanliness or verify disposal rules.

These tests do not execute Unity, ARKit image detection, 3D rendering/animation,
or the device camera. Those require an actual Editor/device run.
