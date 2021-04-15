using System.IO;

using JetBrains.Annotations;

using Sirenix.OdinInspector;

using UnityEditor;

using UnityEngine;

namespace UnityLevelEditor.Editor
{
    using Model;

    public class LevelBuilderSettings : ScriptableObject
    {
        private const string MyCustomSettingsPath = "Assets/Editor/LevelBuilderSettings.asset";
        private const string FolderPath = "Assets/Editor";

#pragma warning disable CS0414
        [SerializeField] public PrefabsPerSide fullWall;
        [SerializeField] public PrefabsPerSide wallShortenedLeft;
        [SerializeField] public PrefabsPerSide wallShortenedRight;
        [SerializeField] public PrefabsPerSide wallShortenedBothSides;
        [SerializeField] public RoomElementSpawnSettings floor;
        [SerializeField] public RoomElementSpawnSettings outerCorner;
        [SerializeField] public RoomElementSpawnSettings innerCorner;
        [SerializeField] public MaterialSlotSetup materialSlotSetup;
        
        [SerializeField] [UsedImplicitly] public string roomName = "StandardRoom";
        [SuffixLabel("UU")]
        [SerializeField] public float floorSize;
        
        [Title("RoomSize")]
        [PropertyRange(1, RoomSpawner.RoomSizeLimit)]
        [LabelText("X")]
        [SuffixLabel("Full Walls")]
        [SerializeField] public int roomSizeX = 1;

        [PropertyRange(1, RoomSpawner.RoomSizeLimit)]
        [LabelText("Z")]
        [SuffixLabel("Full Walls")]
        [SerializeField] public int roomSizeZ = 1;
#pragma warning restore CS0414

        internal static LevelBuilderSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuilderSettings>(MyCustomSettingsPath);

            if (settings == null)
            {
                settings = CreateInstance<LevelBuilderSettings>();

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
}
