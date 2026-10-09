# Tests

The tests use Unity Test Framework / NUnit and are split into EditMode and PlayMode assemblies.

## Coverage

- Horizontal and vertical list alignment, including variable sizes and index clamping.
- Grid navigation and incomplete final rows.
- Cell reuse, rebinding on Refresh, and releasing cells when data becomes empty.
- LazyLayout measurement, shrinking size estimates, and Inspector property binding.
- Loop index mapping and data collection / size-cache consistency.
- Carousel immediate navigation across frames on both axes, bounded navigation, and negative loop positions.

## Run in Unity

Open Window > General > Test Runner. Run the SimpleScroll tests in both the EditMode and PlayMode tabs. The current development project already resolves Unity Test Framework; in another project, install it before running these tests.

When installed as a UPM package, add `com.github.kurobon.simple_scroll` to the `testables` array in Packages/manifest.json to discover package tests.

## Run from the command line

Close the Editor for this project before running batch tests:

```powershell
unity test . --mode EditMode --filter SimpleScroll.Tests --output Temp/test-results/editmode.xml --timeout 180
unity test . --mode PlayMode --filter SimpleScroll.Tests --output Temp/test-results/playmode.xml --timeout 180
```

Test reports are generated under Temp and are not committed. Tests create their own views and prefabs; they do not depend on demo scenes or external sample packages.

The EditMode fixture accesses private setup / update methods through reflection so deterministic layout checks do not require changes to the runtime public API. The PlayMode tests use actual frame updates to check that immediate carousel navigation stays at its target.
