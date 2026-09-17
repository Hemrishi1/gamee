using NUnit.Framework;
using UnityEngine;
using GardenGuardians.Services;

namespace GardenGuardians.Tests.Editor
{
    public class SaveAndApiTests
    {
        [Test]
        public void DefaultSaveData_InitializedWithValidDefaults()
        {
            SaveData defaultSave = SaveData.CreateDefault();

            Assert.AreEqual(1, defaultSave.schemaVersion);
            Assert.AreEqual(1, defaultSave.highestUnlockedLevel);
            Assert.IsTrue(defaultSave.soundEnabled);
            Assert.IsNotNull(defaultSave.bestScores);
            Assert.AreEqual(3, defaultSave.bestScores.Length);
        }

        [Test]
        public void SaveData_SerializationRoundTrip_MaintainsIntegrity()
        {
            SaveData original = new SaveData
            {
                schemaVersion = 1,
                highestUnlockedLevel = 2,
                soundEnabled = false,
                bestScores = new int[] { 150, 200, 0 },
                lastPlayedDate = "2026-09-17"
            };

            string json = JsonUtility.ToJson(original);
            SaveData restored = JsonUtility.FromJson<SaveData>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.highestUnlockedLevel, restored.highestUnlockedLevel);
            Assert.AreEqual(original.soundEnabled, restored.soundEnabled);
            Assert.AreEqual(original.bestScores[0], restored.bestScores[0]);
            Assert.AreEqual(original.bestScores[1], restored.bestScores[1]);
        }

        [Test]
        public void DailyChallenge_MalformedJson_HandlesGracefully()
        {
            string malformedJson = "{ \"date\": \"2026-09-15\", invalid_key ";

            DailyChallengeData result = null;
            try
            {
                result = JsonUtility.FromJson<DailyChallengeData>(malformedJson);
            }
            catch
            {
                result = null;
            }

            Assert.IsNull(result, "Malformed JSON should fail parsing safely without unhandled exception.");
        }

        [Test]
        public void DailyChallenge_ValidJson_ParsesCorrectly()
        {
            string validJson = "{\"date\":\"2026-09-15\",\"startingEnergy\":75,\"wavePreset\":\"fast-rush\"}";

            DailyChallengeData result = JsonUtility.FromJson<DailyChallengeData>(validJson);

            Assert.IsNotNull(result);
            Assert.AreEqual("2026-09-15", result.date);
            Assert.AreEqual(75, result.startingEnergy);
            Assert.AreEqual("fast-rush", result.wavePreset);
        }
    }
}
