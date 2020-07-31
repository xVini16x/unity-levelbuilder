using System.IO;
using UnityEditor;
using UnityEngine;

class LevelBuilderSettings : ScriptableObject
{
    public const string MyCustomSettingsPath = "Assets/Editor/LevelBuilderSettings.asset";
    private const string FolderPath = "Assets/Editor";

    [SerializeField] private GameObject fullWall;
    [SerializeField] private GameObject wallShortenedLeft;
    [SerializeField] private GameObject wallShortenedRight;
    [SerializeField] private GameObject wallShortenedBothSides;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject corner;
    [SerializeField] private Material transparentMaterial;
    [SerializeField] private Material wallSideMaterial;
    [SerializeField] private string roomName;
    [SerializeField] private Vector2 roomSize;

    internal static LevelBuilderSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LevelBuilderSettings>(MyCustomSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LevelBuilderSettings>();
            //default values
            settings.fullWall = null;
            settings.wallShortenedLeft = null;
            settings.wallShortenedRight = null;
            settings.wallShortenedBothSides = null;
            settings.floor = null;
            settings.corner = null;
            settings.transparentMaterial = null;
            settings.wallSideMaterial = null;
            settings.roomName = "StandardRoom";
            settings.roomSize = new Vector2(1, 1);

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

class AssetSettingsProviderRegister
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