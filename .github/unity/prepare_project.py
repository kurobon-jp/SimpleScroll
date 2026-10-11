"""Create an isolated UPM consumer; never downgrade the development project."""

import json
from pathlib import Path
import re
import shutil
import sys


def prepare(version):
    if not re.fullmatch(r"\d+\.\d+\.\d+f\d+", version):
        raise ValueError(f"Expected a full stable Unity version, got {version!r}")

    root = Path(__file__).resolve().parents[2]
    project = root / "Temp" / "unity-compile"
    # Fail on reuse rather than silently keeping stale scripts or settings.
    project.mkdir(parents=True, exist_ok=False)
    (project / "Assets" / "Tests").mkdir(parents=True)
    (project / "Packages").mkdir()
    (project / "ProjectSettings").mkdir()

    package = json.loads((root / "Assets" / "package.json").read_text(encoding="utf-8"))
    manifest = {
        "dependencies": {
            # Local package paths are relative to the project's Packages folder.
            package["name"]: "file:../../../Assets",
            "com.unity.ugui": "2.0.0" if int(version.split(".")[0]) >= 6000 else "1.0.0",
            "com.unity.test-framework": "1.1.33",
            "com.unity.modules.audio": "1.0.0",
            "com.unity.modules.imgui": "1.0.0",
            "com.unity.modules.ui": "1.0.0",
            "com.unity.modules.uielements": "1.0.0",
        }
    }
    (project / "Packages" / "manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )
    (project / "ProjectSettings" / "ProjectVersion.txt").write_text(
        f"m_EditorVersion: {version}\n", encoding="utf-8"
    )
    # Unity ignores Samples~ until imported. Copy scripts and their asmdef so
    # demo API usage is compiled, without importing scene/prefab dependencies.
    samples = root / "Assets" / "Samples~"
    target = project / "Assets" / "SimpleScrollSamples"
    shutil.copytree(samples / "Scripts", target / "Scripts")
    for source in samples.glob("*.asmdef*"):
        shutil.copy2(source, target / source.name)
    shutil.copy2(
        root / ".github" / "unity" / "CompilationCheck.cs",
        project / "Assets" / "Tests" / "CompilationCheck.cs",
    )
    (project / "Assets" / "Tests" / "SimpleScrollCI.asmdef").write_text(
        json.dumps({
            "name": "SimpleScrollCI",
            "references": [
                "Unity.SimpleScroll", "Unity.SimpleScroll.Editor",
                "Unity.SimpleScroll.Samples",
            ],
            "includePlatforms": ["Editor"],
            "optionalUnityReferences": ["TestAssemblies"],
            "autoReferenced": False,
        }, indent=2) + "\n", encoding="utf-8"
    )
    print(f"Prepared {project} for Unity {version}")


if __name__ == "__main__":
    prepare(sys.argv[1])
