using System.IO;
using CubeTower.Runtime.Progress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CubeTower.Editor
{
    /// <summary>
    /// Editor conveniences: Play always starts from the bootstrap scene, progress can be wiped from the menu.
    /// </summary>
    [InitializeOnLoad]
    public static class CubeTowerEditorMenu
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string PlayFromBootstrapMenu = "Cube Tower/Play From Bootstrap";
        private const string PlayFromBootstrapPref = "CubeTower.PlayFromBootstrap";

        static CubeTowerEditorMenu() => EditorApplication.delayCall += ApplyPlayModeStartScene;

        private static bool PlayFromBootstrap
        {
            get => EditorPrefs.GetBool(PlayFromBootstrapPref, true);
            set => EditorPrefs.SetBool(PlayFromBootstrapPref, value);
        }

        [MenuItem(PlayFromBootstrapMenu)]
        private static void TogglePlayFromBootstrap()
        {
            PlayFromBootstrap = !PlayFromBootstrap;
            ApplyPlayModeStartScene();
        }

        [MenuItem(PlayFromBootstrapMenu, true)]
        private static bool TogglePlayFromBootstrapValidate()
        {
            Menu.SetChecked(PlayFromBootstrapMenu, PlayFromBootstrap);
            return true;
        }

        [MenuItem("Cube Tower/Clear Saved Progress")]
        private static void ClearProgress()
        {
            var directory = JsonFileStorage.DefaultDirectory;
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            Debug.Log($"Saved progress cleared: {directory}");
        }

        [MenuItem("Cube Tower/Open Saves Folder")]
        private static void OpenSavesFolder()
        {
            Directory.CreateDirectory(JsonFileStorage.DefaultDirectory);
            EditorUtility.RevealInFinder(JsonFileStorage.DefaultDirectory);
        }

        private static void ApplyPlayModeStartScene()
        {
            EditorSceneManager.playModeStartScene = PlayFromBootstrap
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath)
                : null;
        }
    }
}
