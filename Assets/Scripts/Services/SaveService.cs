using System;
using System.IO;
using UnityEngine;

namespace GardenGuardians.Services
{
    [Serializable]
    public class SaveData
    {
        public int schemaVersion = 1;
        public int highestUnlockedLevel = 1;
        public int[] bestScores = new int[3];
        public bool soundEnabled = true;
        public string lastPlayedDate = "";

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                schemaVersion = 1,
                highestUnlockedLevel = 1,
                bestScores = new int[] { 0, 0, 0 },
                soundEnabled = true,
                lastPlayedDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };
        }
    }

    /// <summary>
    /// Handles robust local persistence using atomic file writing to protect against mobile app suspension / crash corruption.
    /// </summary>
    public static class SaveService
    {
        private const string SaveFileName = "save.json";
        private const string TempFileName = "save.json.tmp";

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private static string TempFilePath => Path.Combine(Application.persistentDataPath, TempFileName);

        public static SaveData CurrentData { get; private set; }

        public static SaveData Load()
        {
            if (CurrentData != null) return CurrentData;

            string path = SaveFilePath;
            if (!File.Exists(path))
            {
                Debug.Log("[SaveService] No save file found. Initializing with default progress.");
                CurrentData = SaveData.CreateDefault();
                Save(CurrentData);
                return CurrentData;
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);

                if (loaded == null || loaded.schemaVersion < 1)
                {
                    Debug.LogWarning("[SaveService] Invalid schema version or corrupted data. Using safe fallback defaults.");
                    CurrentData = SaveData.CreateDefault();
                }
                else
                {
                    CurrentData = loaded;
                    Debug.Log($"[SaveService] Save loaded. Highest Unlocked Level: {CurrentData.highestUnlockedLevel}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error reading save file: {ex.Message}. Falling back to default data.");
                CurrentData = SaveData.CreateDefault();
            }

            return CurrentData;
        }

        public static bool Save(SaveData data)
        {
            if (data == null) return false;

            try
            {
                data.lastPlayedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string json = JsonUtility.ToJson(data, true);

                // Atomic write via temp file
                string tempPath = TempFilePath;
                string destPath = SaveFilePath;

                File.WriteAllText(tempPath, json);

                if (File.Exists(destPath))
                {
                    File.Replace(tempPath, destPath, null);
                }
                else
                {
                    File.Move(tempPath, destPath);
                }

                CurrentData = data;
                Debug.Log("[SaveService] Progress saved atomically.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to write save file: {ex.Message}");
                return false;
            }
        }
    }
}
