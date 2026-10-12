using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace UnityPlayerCompilation
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
                if (result.assemblies != null && result.assemblies.Count != 0 && result.typeDB != null)
                {
                    Debug.Log("Build succeeded!");
                    EditorApplication.Exit(0);
                }

                throw new InvalidOperationException(
                    "Windows Player compilation did not produce any assemblies or type database.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
