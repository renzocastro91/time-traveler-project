using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TimeTraveler.EditorTools
{
    /// <summary>
    /// Preparación y compilación del proyecto. También se usan desde la línea de comandos:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod TimeTraveler.EditorTools.BuildTools.BuildWindows
    /// </summary>
    public static class BuildTools
    {
        public const string MainScene = "Assets/Scenes/Main.unity";
        public const string WindowsBuild = "Builds/Windows/LasHorasQueFaltan.exe";

        [MenuItem("Time Traveler/Preparar proyecto (escena y shaders)")]
        public static void PrepareProject()
        {
            EnsureMainScene();
            // Los modelos provisionales crean materiales por código: el shader debe ir en la compilación.
            EnsureAlwaysIncluded("Standard");
            EnsureAlwaysIncluded("Legacy Shaders/Diffuse");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Time Traveler/Compilar para Windows")]
        public static void BuildWindows()
        {
            PrepareProject();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = WindowsBuild,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log("[Time Traveler] Compilación: " + report.summary.result + " (" + report.summary.totalErrors + " errores) → " + WindowsBuild);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Escena principal vacía: el juego se construye solo al arrancar (GameManager).</summary>
        private static void EnsureMainScene()
        {
            if (!File.Exists(MainScene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MainScene));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, MainScene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
        }

        private static void EnsureAlwaysIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) return;
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedProperties();
        }
    }
}
