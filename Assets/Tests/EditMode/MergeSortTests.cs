using System.Collections.Generic;
using NUnit.Framework;
using TheLostDungeon.Algorithms;
using TheLostDungeon.Online;

namespace TheLostDungeon.Tests
{
    public class MergeSortTests
    {
        [Test]
        public void Sort_OrdersNumbersAscending()
        {
            var numbers = new List<int> { 5, 3, 9, 1, 7, 2, 8 };
            MergeSort.Sort(numbers, (a, b) => a.CompareTo(b));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 5, 7, 8, 9 }, numbers);
        }

        [Test]
        public void Sort_HandlesEmptySingleAndDuplicates()
        {
            var empty = new List<int>();
            MergeSort.Sort(empty, (a, b) => a.CompareTo(b));
            Assert.IsEmpty(empty);

            var single = new List<int> { 4 };
            MergeSort.Sort(single, (a, b) => a.CompareTo(b));
            CollectionAssert.AreEqual(new[] { 4 }, single);

            var duplicates = new List<int> { 3, 1, 3, 1, 2 };
            MergeSort.Sort(duplicates, (a, b) => a.CompareTo(b));
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 3, 3 }, duplicates);
        }

        [Test]
        public void Sort_IsStable_EqualItemsKeepTheirOrder()
        {
            // (score, name): two pairs of equal scores.
            var entries = new List<(int score, string name)> { (10, "a"), (30, "b"), (10, "c"), (30, "d") };
            MergeSort.Sort(entries, (x, y) => y.score.CompareTo(x.score));   // descending by score
            CollectionAssert.AreEqual(new[] { "b", "d", "a", "c" }, entries.ConvertAll(e => e.name));
        }

        [Test]
        public void Sorted_LeavesInputUntouched()
        {
            var input = new List<int> { 3, 2, 1 };
            List<int> output = MergeSort.Sorted(input, (a, b) => a.CompareTo(b));
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, input);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, output);
        }

        [Test]
        public void LeaderboardRanking_HighScoreFirst_TiesByEarlierSubmission()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { name = "Late", score = 500, submittedUtcTicks = 200 },
                new LeaderboardEntry { name = "Best", score = 900, submittedUtcTicks = 300 },
                new LeaderboardEntry { name = "Early", score = 500, submittedUtcTicks = 100 },
            };
            LeaderboardEntry.Rank(entries);
            CollectionAssert.AreEqual(new[] { "Best", "Early", "Late" }, entries.ConvertAll(e => e.name));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, entries.ConvertAll(e => e.rank));
        }

        [Test]
        public void LootLockerListJson_IsParsedIntoEntries()
        {
            const string json = "{\"success\":true,\"items\":[" +
                                "{\"member_id\":\"7\",\"rank\":1,\"score\":1200,\"metadata\":\"{\\\"name\\\":\\\"Ella\\\",\\\"levels\\\":3,\\\"stars\\\":7}\",\"player\":{\"id\":7,\"name\":\"\"}}," +
                                "{\"member_id\":\"9\",\"rank\":2,\"score\":800,\"metadata\":\"\",\"player\":{\"id\":9,\"name\":\"Guest\"}}]}";

            List<LeaderboardEntry> entries = LootLockerLeaderboardService.ParseList(json);

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("Ella", entries[0].name);
            Assert.AreEqual(1200, entries[0].score);
            Assert.AreEqual(3, entries[0].levels);
            Assert.AreEqual("Guest", entries[1].name);   // falls back to the player name
        }
    }
}
