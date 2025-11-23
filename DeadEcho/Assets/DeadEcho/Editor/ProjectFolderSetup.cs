#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectFolderSetup
{
    private static readonly string[] Folders =
    {
        "Assets/Project",
        "Assets/Project/Scenes",
        "Assets/Project/Scripts",
        "Assets/Project/Prefabs",
        "Assets/Project/Materials",
        "Assets/Project/Shaders",
        "Assets/Project/Textures",
        "Assets/Project/Sprites",
        "Assets/Project/Animations",
        "Assets/Project/AnimationControllers",
        "Assets/Project/Audio",
        "Assets/Project/Audio/Music",
        "Assets/Project/Audio/SFX",
        "Assets/Project/UI",
        "Assets/Project/UI/Fonts",
        "Assets/Project/Art",
        "Assets/Project/Resources",
        "Assets/Project/Addressables",
        "Assets/Project/Plugins",
        "Assets/Project/Editor",
        "Assets/Project/Settings",
        "Assets/Project/StreamingAssets",
        "Assets/Project/ThirdParty",
        "Assets/Project/Tests"
    };

    [MenuItem("Tools/Setup/Create Default Project Folders", priority = 0)]
    public static void CreateDefaultProjectFolders()
    {
        int created = 0;
        foreach (var path in Folders)
        {
            created += EnsureUnityFolder(path) ? 1 : 0;
            EnsureGitKeep(path);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Project Setup",
            $"Structure Created/updated!\nProcessed folders: {Folders.Length}\nNew folders: {created}",
            "Ok");
    }

    private static bool EnsureUnityFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath))
            return false;

        var parts = fullPath.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            Debug.LogError($"Invalid path (Needs to starts with Assets): {fullPath}");
            return false;
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
        return true;
    }

    /// <summary>
    /// Creates a .gitkeep file in each folder, in order to mantain the directories empty in version control.
    /// Ignores if already exists any file inside the folder.
    /// </summary>
    private static void EnsureGitKeep(string assetRelativePath)
    {
        string absPath = ToAbsolutePath(assetRelativePath);
        if (string.IsNullOrEmpty(absPath))
            return;

        try
        {
            if (!Directory.Exists(absPath))
                return;

            // Se já tem algum arquivo (além de .meta), não precisa do .gitkeep
            var files = Directory.GetFiles(absPath);
            foreach (var f in files)
            {
                var name = Path.GetFileName(f);
                if (!name.EndsWith(".meta") && !name.Equals(".gitkeep"))
                    return;
            }

            string keep = Path.Combine(absPath, ".gitkeep");
            if (!File.Exists(keep))
            {
                File.WriteAllText(keep, "");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to create .gitkeep in {assetRelativePath}: {e.Message}");
        }
    }

    private static string ToAbsolutePath(string assetRelativePath)
    {
        if (string.IsNullOrEmpty(assetRelativePath) || !assetRelativePath.StartsWith("Assets"))
            return null;

        string projectRoot = Application.dataPath;
        if (assetRelativePath == "Assets")
            return projectRoot;

        string sub = assetRelativePath.Substring("Assets/".Length);
        return Path.Combine(projectRoot, sub);
    }
}
#endif