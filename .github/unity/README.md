# Unity compilation CI

`unity-compile.yml` runs on same-repository pull requests,
and can be started manually from Actions. Fork pull requests are
skipped because GitHub does not provide license secrets to them; run their
changes manually on a trusted branch after review.

The workflow checks Unity 2022.1.25f1 only. There is no version or variant matrix.

Each job creates a fresh project under `Temp/unity-compile`, installs `Assets/`
as a local UPM package, and imports the sample scripts. uGUI is pinned to 1.0.0
for Unity 2022/2023 and 2.0.0 for Unity 6. The development project's settings
and dependencies are not used. Runtime, Editor, and sample assemblies must
all compile and load before the job succeeds. This is an Editor compilation
check using a consumer EditMode test, not a player build. The consumer test
verifies that the Runtime, Editor, and sample assemblies are loaded. The
package's behavioral tests are excluded (the package is not in `testables`).

## One-time repository setup

The workflow uses `game-ci/unity-test-runner@v4`, matching the consumer workflow
that successfully ran with only `UNITY_EMAIL` and `UNITY_PASSWORD`. Configure
those two repository Actions secrets for that activation path.

Existing `UNITY_LICENSE` or `UNITY_SERIAL` secrets are also forwarded when
configured. See [GameCI activation](https://game.ci/docs/github/activation/)
for those activation methods. The preflight accepts `UNITY_LICENSE` or both
`UNITY_EMAIL` and `UNITY_PASSWORD`; it only checks configuration presence,
and does not prove that Unity activation will succeed for every editor version.

GameCI activates and returns the license. Missing credentials fail explicitly.
GitHub Actions logs contain the compiler output; artifacts retain test results,
test-runner logs, the resolved
package manifest, lockfile, project logs when present, and success marker.

## Local preparation check

Run `python .github/unity/prepare_project.py 2022.1.25f1` to inspect the generated
project. The destination must not already exist. Open that generated project
with the matching Unity Editor to compile it locally.
