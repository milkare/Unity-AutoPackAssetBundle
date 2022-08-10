using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Pack : ScriptableWizard
{
    private static BuildAssetBundleOptions buildOptions;

    private static string assetBundlePathDisplay;
    private static string outputPathDisplay;
    private static string assetBundlePath;
    private static string outputPath;
    private static string outputAssetBundleExtension;
    private static string outputSceneAssetBundleExtension;

    private static GUIStyle centerStyleLayout;

    private void Awake()
    {
        centerStyleLayout = new GUIStyle
        {
            richText = false,
            normal =
            {
                textColor = Color.white
            }
        };
        outputSceneAssetBundleExtension = "unity";
        outputAssetBundleExtension = "assetBundle";
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(assetBundlePathDisplay))
        {
            assetBundlePathDisplay = "AssetBundlePath:The path is empty now";
        }

        if (string.IsNullOrEmpty(outputPathDisplay))
        {
            outputPathDisplay = "OutputAssetBundlePath:The path is empty now";
        }
    }

    protected override bool DrawWizardGUI()
    {
        EditorGUILayout.BeginVertical();
        buildOptions= (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("BuildOptions",buildOptions);
        EditorGUILayout.LabelField(assetBundlePathDisplay, centerStyleLayout);
        if (GUILayout.Button("Select Asset Folder"))
        {
            var selectedPath= EditorUtility.OpenFolderPanel("Select the folder which storing your assets", Application.dataPath,"AssetBundle");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                assetBundlePath = selectedPath;
                selectedPath = selectedPath.Substring(Application.dataPath.Length + 1);
                assetBundlePathDisplay = "AssetBundlePath:"+selectedPath;
            }
        }
        EditorGUILayout.LabelField(outputPathDisplay, centerStyleLayout);
        if (GUILayout.Button("Select Output Folder"))
        {
            var selectedPath = EditorUtility.OpenFolderPanel("Select the folder which you want to put assetBundle in", Application.streamingAssetsPath, "AssetBundle");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                outputPath = selectedPath;
                selectedPath = selectedPath.Substring(Application.dataPath.Length + 1);
                outputPathDisplay = "OutputAssetBundlePath:" + selectedPath;
            }
        }

        outputAssetBundleExtension = EditorGUILayout.TextField("AssetBundle extension", outputAssetBundleExtension);
        outputSceneAssetBundleExtension = EditorGUILayout.TextField("Scene assetBundle extension", outputSceneAssetBundleExtension);
        EditorGUILayout.EndVertical();
        return base.DrawWizardGUI();
    }

    private void OnWizardCreate()
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            Debug.LogError("Didn't select the output folder!");
            return;
        }

        if (string.IsNullOrEmpty(outputAssetBundleExtension)||string.IsNullOrEmpty(outputSceneAssetBundleExtension))
        {
            Debug.LogError("Must fill the assetBundle extension!");
            return;
        }
        errorString = string.Empty;
        DeleteOldAssetBundle();
        BuildAssetBundle();
        Debug.Log("Build successfully!");
    }
    
    private void OnWizardOtherButton()
    {
        if (string.IsNullOrEmpty(assetBundlePath))
        {
            errorString = "Didn't select the asset folder!";
            return;
        }

        errorString = string.Empty;
        if (DeleteMarkedLabel())
        {
            MarkFile();
            AssetDatabase.Refresh();
            Debug.Log("All selected files have been marked.");
        }
        else
        {
            throw new AccessViolationException("Didn't delete all assetBundle label.Try again.");
        }
    }

    private static bool DeleteMarkedLabel()
    {
        var labels = AssetDatabase.GetAllAssetBundleNames();
        var succeedDeleteCount = labels.Select(label => AssetDatabase.RemoveAssetBundleName(label, true))
            .Count(isSuccessful => isSuccessful);
        return labels.Length==succeedDeleteCount;
    }

    private static void MarkFile()
    {
        var rootFolder = new DirectoryInfo(assetBundlePath);
        var childrenFolder= rootFolder.GetDirectories();
        var fileList = new Dictionary<DirectoryInfo,List<FileInfo>>();
        foreach (var sceneDirectoryInfo in childrenFolder)
        {
            var sceneFileList = new List<FileInfo>();
            GetAllFiles(sceneDirectoryInfo,ref sceneFileList);
            fileList.Add(sceneDirectoryInfo,sceneFileList);
        }

        foreach (var value in fileList.Select(keyValuePair => keyValuePair.Value))
        {
            value.RemoveAll(file => file.Extension == ".meta");
        }

        foreach (var value in fileList.Select(keyValuePair => keyValuePair.Value))
        {
            var key = fileList.Where(list => list.Value == value).Select(key => key.Key).FirstOrDefault();
            foreach (var fileInfo in value)
            {
                var index = fileInfo.FullName.IndexOf("Assets", StringComparison.Ordinal);
                var path = fileInfo.FullName.Substring(index);
                var assetImporter= AssetImporter.GetAtPath(path);
                if (key == null)
                    throw new NullReferenceException("Didn't find the same value in list!");
                var startIndex = fileInfo.FullName.IndexOf(key.Name, StringComparison.Ordinal) + key.Name.Length + 1;
                var bundlePath = fileInfo.FullName.Substring(startIndex).Split('\\');
                assetImporter.assetBundleName = key.Name + "/" + bundlePath[0];
                assetImporter.assetBundleVariant = fileInfo.Extension==".unity" ? outputSceneAssetBundleExtension : outputAssetBundleExtension;
            }
        }
    }

    private static void GetAllFiles(DirectoryInfo folder,ref List<FileInfo> fileList)
    {
        fileList.AddRange(folder.GetFiles());
        foreach (var directoryInfo in folder.GetDirectories())
        {
            GetAllFiles(directoryInfo,ref fileList);
        }
    }

    private static void DeleteOldAssetBundle()
    {
        if (!Directory.Exists(outputPath)) return;
        Directory.Delete(outputPath, true);
        File.Delete(outputPath + ".meta");
        AssetDatabase.Refresh();
    }

    private static void BuildAssetBundle()
    {
        Directory.CreateDirectory(outputPath);
        BuildPipeline.BuildAssetBundles(outputPath, buildOptions, BuildTarget.StandaloneWindows64);
        AssetDatabase.Refresh();
    }

    [MenuItem("Asset/PackAsset")]
    public static void PackAsset()
    {
        DisplayWizard<Pack>("PackAsset", "StartBuild", "MarkFile");
    }
}
