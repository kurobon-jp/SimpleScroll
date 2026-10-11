# Unity compilation CI

`unity-compile.yml` runs on package changes in pushes and same-repository pull
requests, and can be started manually from Actions. Fork pull requests are
skipped because GitHub does not provide license secrets to them; run their
changes on a trusted branch after review.

The matrix pins one patch per release series: 2022.1, 2022.2, 2022.3, 2023.1,
2023.2, and Unity 6.0–6.4. It does not test every patch or future releases.
To extend coverage, add an exact stable editor version to the matrix and verify
that `unityci/editor:ubuntu-<version>-base-3` exists on Docker Hub.

Each job creates a fresh project under `Temp/unity-compile`, installs `Assets/`
as a local UPM package, and imports the sample scripts. uGUI is pinned to 1.0.0
for Unity 2022/2023 and 2.0.0 for Unity 6. The development project's settings
and dependencies are not used. Runtime, Editor, and sample assemblies must
all compile and load before the job succeeds. This is an Editor compilation
check, not a player build or a test run. NUnit test assemblies are excluded.

## One-time repository setup

Follow [GameCI activation](https://game.ci/docs/github/activation/) and configure
repository Actions secrets:

- Personal: `UNITY_LICENSE` (the contents of the `.ulf` file), `UNITY_EMAIL`,
  and `UNITY_PASSWORD`.
- Pro: `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.

Jobs run sequentially to limit simultaneous license activations. GameCI
activates and returns the license. Missing credentials fail explicitly.
GitHub Actions logs contain the compiler output; artifacts retain the resolved
package manifest, lockfile, project logs when present, and success marker.

## Local preparation check

Run `python .github/unity/prepare_project.py 2022.1.25f1` to inspect the generated
project. The destination must not already exist. Open that generated project
with the matching Unity Editor to compile it locally.
