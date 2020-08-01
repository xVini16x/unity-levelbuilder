using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

internal class LevelBuilderSettings : ScriptableObject
{
    private const string MyCustomSettingsPath = "Assets/Editor/LevelBuilderSettings.asset";
    private const string FolderPath = "Assets/Editor";

#pragma warning disable CS0414
    [SerializeField] [UsedImplicitly] private GameObject fullWall;
    [SerializeField] [UsedImplicitly] private GameObject wallShortenedLeft;
    [SerializeField] [UsedImplicitly] private GameObject wallShortenedRight;
    [SerializeField] [UsedImplicitly] private GameObject wallShortenedBothSides;
    [SerializeField] [UsedImplicitly] private GameObject floor;
    [SerializeField] [UsedImplicitly] private GameObject corner;
    [SerializeField] [UsedImplicitly] private Material transparentMaterial;
    [SerializeField] [UsedImplicitly] private Material wallSideMaterial;
    [SerializeField] [UsedImplicitly] private string roomName;
    [SerializeField] [UsedImplicitly] private Vector2Int roomSize;
#pragma warning restore CS0414
    
    internal static LevelBuilderSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LevelBuilderSettings>(MyCustomSettingsPath);
        
        if (settings == null)
        {
            settings = CreateInstance<LevelBuilderSettings>();
            //default values
            settings.roomName = "StandardRoom";
            settings.roomSize = new Vector2Int(1, 1);

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            AssetDatabase.CreateAsset(settings, MyCustomSettingsPath);
            AssetDatabase.SaveAssets();
        }

        return settings;
    }

    internal static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
}

internal static class AssetSettingsProviderRegister
{
    private const string ProviderPath = "Project/LevelBuilderSettings";

    [SettingsProvider]
    public static SettingsProvider CreateFromSettingsObject()
    {
        // Create an AssetSettingsProvider from a settings object (UnityEngine.Object):
        var settingsObj = LevelBuilderSettings.GetOrCreateSettings();

        var provider = AssetSettingsProvider.CreateProviderFromObject(ProviderPath, settingsObj);

        // Register keywords from the properties of MyCustomSettings
        provider.keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(settingsObj));
        return provider;
    }

}