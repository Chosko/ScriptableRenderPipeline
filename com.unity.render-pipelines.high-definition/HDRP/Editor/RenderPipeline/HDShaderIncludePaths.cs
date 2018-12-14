using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class HDIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            var paths = new string[3];
            paths[0] = Path.GetFullPath("Packages/com.unity.render-pipelines.high-definition");
            paths[1] = Path.GetFullPath("Assets/ScriptableRenderPipeline/com.unity.render-pipelines.core");
            paths[2] = Path.GetFullPath("Assets/ScriptableRenderPipeline/com.unity.render-pipelines.high-definition");
            return paths;
        }
    }
}
