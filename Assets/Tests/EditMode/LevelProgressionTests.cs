using NUnit.Framework;
using TheLostDungeon.Data;
using TheLostDungeon.Levels;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "Level unlocking" test area.</summary>
    public class LevelProgressionTests
    {
        private const int LevelCount = 5;

        private static LevelProgression NewProgression(out SaveData data)
        {
            data = new SaveData();
            return new LevelProgression(data, LevelCount);
        }

        [Test]
        public void NewSave_FirstLevelAvailable_OthersLocked()
        {
            LevelProgression progression = NewProgression(out _);

            Assert.AreEqual(LevelStatus.Available, progression.GetStatus(0));
            for (int i = 1; i < LevelCount; i++)
            {
                Assert.AreEqual(LevelStatus.Locked, progression.GetStatus(i), $"Level {i + 1} should start locked");
            }
        }

        [Test]
        public void CompletingLevel_MarksCompleted_AndUnlocksNext()
        {
            LevelProgression progression = NewProgression(out _);

            bool accepted = progression.CompleteLevel(0, 120f, 3, 500, out bool unlockedNewLevel);

            Assert.IsTrue(accepted);
            Assert.IsTrue(unlockedNewLevel);
            Assert.AreEqual(LevelStatus.Completed, progression.GetStatus(0));
            Assert.AreEqual(LevelStatus.Available, progression.GetStatus(1));
            Assert.AreEqual(LevelStatus.Locked, progression.GetStatus(2));
        }

        [Test]
        public void LockedLevel_CannotBeCompleted()
        {
            LevelProgression progression = NewProgression(out _);

            bool accepted = progression.CompleteLevel(3, 60f, 3, 999, out _);

            Assert.IsFalse(accepted);
            Assert.AreEqual(LevelStatus.Locked, progression.GetStatus(3));
        }

        [Test]
        public void ReplayingLevel_KeepsBestTimeStarsAndScore()
        {
            LevelProgression progression = NewProgression(out _);

            progression.CompleteLevel(0, 100f, 2, 400, out _);
            progression.CompleteLevel(0, 140f, 1, 600, out bool unlockedAgain);

            LevelProgressData record = progression.GetRecord(0);
            Assert.AreEqual(100f, record.bestTimeSeconds, 0.001f, "Slower run must not overwrite best time");
            Assert.AreEqual(2, record.bestStars);
            Assert.AreEqual(600, record.bestScore);
            Assert.IsFalse(unlockedAgain, "Replaying shouldn't report a new unlock");
        }

        [Test]
        public void NextLevelToPlay_IsFirstUnfinishedLevel()
        {
            LevelProgression progression = NewProgression(out _);
            progression.CompleteLevel(0, 90f, 3, 100, out _);
            progression.CompleteLevel(1, 90f, 3, 100, out _);

            Assert.AreEqual(2, progression.NextLevelToPlay);
        }

        [TestCase(100f, 120f, 3)]
        [TestCase(120f, 120f, 3)]
        [TestCase(170f, 120f, 2)]
        [TestCase(200f, 120f, 1)]
        public void CalculateStars_UsesParTime(float time, float par, int expectedStars)
        {
            Assert.AreEqual(expectedStars, LevelProgression.CalculateStars(time, par));
        }
    }
}
