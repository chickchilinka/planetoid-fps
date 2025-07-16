using UnityEngine;

namespace Utils
{
    public class Const
    {
        public static class Url
        {
            
        }

        public const string DateTimeParseFormat = "dd'.'MM'.'yyyy HH':'mm";

        public static class Token
        {
#if DEV||TEST
            public const string AuthToken = "247032b0-ffe0-4166-b824-c5ca479529d4";
#elif PROD
            public const string AuthToken = "c7ae9d36-1a0d-4165-90ad-3ac77f33c6f8";
#endif
        }

        public static string AppleStoreId => "6467385589";
        
        public static class Scene
        {
            // public const string TourBus = "TourBus";
        }
        
        public static class Environment
        {
            public const string Dev = "DEV";
            public const string Test = "TEST";
            public const string Prod = "PROD";
        }
        
        public static class Path
        {
            public const string ResourcesPrefabsWindow = "Prefabs/Window/";
            public const string ResourcesPrefabsEffects = "Prefabs/Effects/";
            public const string ResourcesPrefabsCountries = "Prefabs/Countries/";
            public const string ResourcesPrefabsCharacters = "Prefabs/Characters/";
            public const string ResourcesVideoClip = "Prefabs/Video/";
            public const string ResourcesModels = "Prefabs/Models/";
            public const string ResourcesMaterials = "Prefabs/Materials/";
            public const string ResourcesAnimation = "Prefabs/Animation/";
            public const string ResourcesCharacters = "Prefabs/Characters/";
            public const string Instruments = "Instruments/";

            public static string LevelsFolder => "Levels/";
            public static string TestLevelsFolder => "TestLevels/";
            public static string TestLevelFolderFullPath = Application.dataPath + "/Resources/" + Path.TestLevelsFolder;

            public const string AssetExtension = ".asset";
            public const string DatabaseLocalRegistriesPath = "Assets/Data/Local/";
            public const string DatabaseRemoteRegistriesPath = "Assets/Data/Remote/";
            public const string DatabaseGameDataImporterPath = "Assets/Data/Importer/GameDataImporter.asset";
            public const string DatabaseLocalizationImporterPath = "Assets/Data/Importer/LocalizationImporter.asset";
            public const string DatabaseGameSettingsRegistryPath = "Assets/Resources/GameSettingsRegistry.asset";

            public const string DatabaseLevelsRegistriesPath = "Assets/Data/Local/Levels";
        }
        
        public static class Labels
        {
            public const string DefaultTimeFormat = "d:h:m:s";
            public const string DefaultDateTime = "01/01/1601 00:00:00";
            public const string DefaultTimerFormat = "{0}:{1:00}"; // mm:ss

            public const string CurrencyFormat = "{0} <sprite name={1}>";
            public const string AdsFormat = "<sprite name={0}>";
            public const string InstrumentFullIconFormat = "full_{0}";
            public const string SpriteFormat = "<sprite name={0}>";
            public const string BrKey = "<br>";
            public const string EnterKey = "\n";
            public const string Underline = "_";

            public const string MovesLeftFormat = "<sprite name=Moves> {0}";
            public const string TimeLeftFormat = "<sprite name=Timer> {0}";

            public const string PosterFormat = "Poster_{0}_{1}";

            public const string UnderlineFormat = "{0}_{1}";
            public const string DashFormat = "{0} - {1}";

            public const string MoreFormat = "> {0}";
            public const string LessFormat = "< {0}";
            
            public const string FieldActionsFileNameFormat = "{0}_record";
            public const string FieldStateFileNameFormat = "{0}_field";

            public const string ComboEffectFormat = "Combo_{0}";
        }
        
        public static class LocalizationKeys
        {
            public const string LoadingFormatKey = "loadingKey";
            public const string AssetsLoadingKey = "assetsLoadingKey";
        }
        
        public static class Values
        {
            public const float Threshold = 0.01f;
            public const float BytesInMb = 1000000f;
            public const int InvalidIndex = -1;
            public const int InvalidInt = -100;
            public const int FieldMaxSize = 9;
            public const int DefaultModelLevel = 1;
            public const int DefaultDamage = 1;
            public const int DefaultBoostsCount = 1;
            public const int DefaultTileLevel = 0;
            public const int DefaultStickerLives = 0;
            public const float ChargeEffectProbability = 0.05f;
            public const float MaxShufflePercentOfField = 1;
            public const float ConcertLevelIconMultiplier = 1.5f;
            public const float DefaultLevelIconMultiplier = 1f;
            public const int DefaultSeed = 0;
            public const int DefaultWeight = 100;
            public const int MaxPercents = 100;
            public const int DefaultTileAmount = 1;
            public const int DefaultKeysAmount = 1;
            public const float DefaultTileSize = 256f;
            public const int InfinityLevelRestrictionAmount = 0;
            public const int InfinityStickerLives = -1;

            public const int DefaultCommandDuration = 1;
            public static readonly Vector3 DefaultTileScale3 = Vector3.one * 100f / DefaultTileSize;
            public static readonly Vector2 DefaultTileScale2 = Vector2.one * 100f / DefaultTileSize;
            public static readonly float DefaultTileScale = 100f / DefaultTileSize;
        }
        
        public static class Signs
        {
            public const char Plus = '+';
            public const char TimeSeparator = ':';
            public const char DataSeparator = ';';
            public const char Slash = '/';
            public const char Dot = '.';
            public const char TileLevelSeparator = '_';
            public const char Underline = '_';
            public const char Caret = '^';
            public const char Percent = '%';
            public const char Dash = '-';
        }
        
        public static class Keys
        {
            public const string InKey = "in";
            public const string OutKey = "out";
            
            public const string BoosterEffectFormat = "{0}BoosterEffect";
            
            public const string EditorLevel = "EditorLevelKey";
            public const string ActiveMovesRecord = "ActiveMovesRecordKey";
            public const string MovesRecords = "MovesRecordsKey";

            public const string AddressableDefaultAssetKey = "Common";
            public const string InfinityKey = "Infinity";

            public const string FieldActionsRecorderJson = "ActiveMovesRecordKey";

            public const string PrefsPlayerDataKey = "PlayerData";
            public const string PrefsGameDataKey = "GameData";
        }

        public static class Events
        {
            public const string EventDataFormat = "[{0}] {1}";
            public const string Equipped = "Equipped";
            public const string SlotFormat = "Slot_{0}";
        }

        public static class Atlas
        {
            public const string CoreAtlas = "CoreAtlas";
            public const string UIAtlas = "UIAtlas";
            public const string LocalAtlasAssetLabel = "SpriteAtlas";
        }

        public static class Addressables
        {
            public const string BoosterAssetKeyFormat = "booster_{0}";

            public const string AddressableDefaultAssetKey = "Common";
            public const string AddressableDownloadAssetKey = "Download";
        }
    }
}
