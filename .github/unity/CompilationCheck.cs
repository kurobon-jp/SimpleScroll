using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SimpleScrollCI
{
    public class CompilationCheck
    {
        [Test]
        public void PackageAndSamplesAreCompiled()
        {
            // Compilation errors prevent test discovery/execution. Explicitly
            // check the expected assemblies to avoid an empty consumer passing.
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
                Assert.That(loaded, Does.Contain(name), "Missing compiled assembly: " + name);
            }

            var message = "Unity " + Application.unityVersion + ": compiled " +
                string.Join(", ", required);
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "compile-success.txt"),
                message + Environment.NewLine);
            Debug.Log(message);
        }
    }
}
