using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SimpleScrollCI
{
    public static class CompilationCheck
    {
        public static void Run()
        {
            // Unity compiles scripts before executeMethod. Compile errors prevent
            // this method from running and cause a nonzero batch-mode exit.
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName().Name).ToArray();
            var required = new[]
            {
                "Unity.SimpleScroll",
                "Unity.SimpleScroll.Editor",
                "Unity.SimpleScroll.Samples"
            };
            foreach (var name in required)
            {
                if (!loaded.Contains(name))
                    throw new InvalidOperationException("Missing compiled assembly: " + name);
            }

            var message = "Unity " + Application.unityVersion + ": compiled " +
                string.Join(", ", required);
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "compile-success.txt"),
                message + Environment.NewLine);
            Debug.Log(message);
            EditorApplication.Exit(0);
        }
    }
}
