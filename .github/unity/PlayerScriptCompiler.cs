using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace SimpleScrollCI
{
    public static class PlayerScriptCompiler
    {
        public static void CompileWindows()
        {
            try
            {
                const string output = "Temp/PlayerScripts/StandaloneWindows64";
                Directory.CreateDirectory(output);
                var settings = new ScriptCompilationSettings
                {
                    target = BuildTarget.StandaloneWindows64,
                    group = BuildTargetGroup.Standalone,
                    options = ScriptCompilationOptions.None
                };
                var result = PlayerBuildInterface.CompilePlayerScripts(settings, output);
                if (result.assemblies == null ||
                    !result.assemblies.Any(path =>
                        Path.GetFileName(path) == "Unity.SimpleScroll.dll") ||
                    !File.Exists(Path.Combine(output, "Unity.SimpleScroll.dll")))
                {
                    throw new InvalidOperationException(
                        "Windows Player compilation did not produce Unity.SimpleScroll.dll.");
                }

                Debug.Log("Windows Player scripts compiled successfully: " + output);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
