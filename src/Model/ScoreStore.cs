using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace LightsOutCube.Model
{
    public static class ScoreStore
    {
        private static readonly string DirectoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LightsOutCube");
        private static readonly string FilePath = Path.Combine(DirectoryPath, "scores.json");
        private static readonly string SpeedRunFilePath = Path.Combine(DirectoryPath, "speedrun.json");
        private static readonly object Sync = new();

        // --- per-puzzle best scores (existing) ---
        public static IList<ScoreRecord> LoadAll()
        {
            lock (Sync)
            {
                if (!File.Exists(FilePath))
                    return new List<ScoreRecord>();

                using (var stream = File.OpenRead(FilePath))
                {
                    var ser = new DataContractJsonSerializer(typeof(List<ScoreRecord>));
                    try
                    {
                        return (List<ScoreRecord>)ser.ReadObject(stream) ?? new List<ScoreRecord>();
                    }
                    catch
                    {
                        // Corrupt file — replace with empty list
                        return new List<ScoreRecord>();
                    }
                }
            }
        }

        public static void Add(ScoreRecord record)
        {
            lock (Sync)
            {
                var list = LoadAll().ToList();
                list.Add(record);
                SaveList(list);
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
        }

        private static void SaveList(IList<ScoreRecord> list)
        {
            lock (Sync)
            {
                Directory.CreateDirectory(DirectoryPath);
                using (var stream = File.Create(FilePath))
                {
                    var ser = new DataContractJsonSerializer(typeof(List<ScoreRecord>));
                    ser.WriteObject(stream, list);
                }
            }
        }

        /// <summary>
        /// Returns the best (fastest) record for the given puzzle id, or null when none exists.
        /// </summary>
        public static ScoreRecord GetBestRecord(string puzzleId)
        {
            if (string.IsNullOrEmpty(puzzleId)) return null;
            var list = LoadAll();
            return list
                .Where(r => string.Equals(r.PuzzleId, puzzleId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Duration)
                .FirstOrDefault();
        }

        /// <summary>
        /// Adds the record only if it is the first record for the puzzle or faster than the existing best.
        /// Returns true when the store was updated (insert or replace), false when the new record was not better.
        /// </summary>
        public static bool AddIfBest(ScoreRecord record)
        {
            if (record == null) return false;
            if (string.IsNullOrEmpty(record.PuzzleId))
                record.PuzzleId = "unknown";

            lock (Sync)
            {
                var list = LoadAll().ToList();
                var existing = list.FirstOrDefault(r => string.Equals(r.PuzzleId, record.PuzzleId, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    list.Add(record);
                    SaveList(list);
                    return true;
                }

                // If new time is strictly faster, replace the stored best
                if (record.Duration < existing.Duration)
                {
                    list.Remove(existing);
                    list.Add(record);
                    SaveList(list);
                    return true;
                }

                // Not a new best — do nothing
                return false;
            }
        }

        // --- speed-run summary persistence ---

        /// <summary>
        /// Saves the last completed speed run summary by adding it to the persisted list of runs.
        /// Appends the new run to the end of the list (oldest -> newest).
        /// </summary>
        public static void SaveLastSpeedRun(SpeedRunSummary summary)
        {
            if (summary == null) return;

            lock (Sync)
            {
                Directory.CreateDirectory(DirectoryPath);
                try
                {
                    // Load existing stats (if any) and append the new summary.
                    ScoreStats stats = null;
                    if (File.Exists(SpeedRunFilePath))
                    {
                        try
                        {
                            using (var inStream = File.OpenRead(SpeedRunFilePath))
                            {
                                var reader = new DataContractJsonSerializer(typeof(ScoreStats));
                                stats = (ScoreStats)reader.ReadObject(inStream);
                            }
                        }
                        catch
                        {
                            // Attempt a best-effort migration: if the file contained a single SpeedRunSummary,
                            // read it and convert to ScoreStats.
                            try
                            {
                                using (var inStream = File.OpenRead(SpeedRunFilePath))
                                {
                                    var readerSingle = new DataContractJsonSerializer(typeof(SpeedRunSummary));
                                    var single = (SpeedRunSummary)readerSingle.ReadObject(inStream);
                                    if (single != null)
                                    {
                                        stats = new ScoreStats();
                                        stats.SpeedRuns.Add(single);
                                    }
                                }
                            }
                            catch
                            {
                                // ignore and create fresh stats below
                            }
                        }
                    }

                    if (stats == null)
                        stats = new ScoreStats();

                    stats.SpeedRuns.Add(summary);

                    using (var outStream = File.Create(SpeedRunFilePath))
                    {
                        var writer = new DataContractJsonSerializer(typeof(ScoreStats));
                        writer.WriteObject(outStream, stats);
                    }
                }
                catch
                {
                    // best-effort: ignore persistence errors
                }
            }
        }

        /// <summary>
        /// Loads the last saved speed run summary (newest run), or null if none available or on error.
        /// </summary>
        public static SpeedRunSummary LoadLastSpeedRun()
        {
            lock (Sync)
            {
                if (!File.Exists(SpeedRunFilePath))
                    return null;

                try
                {
                    // Prefer the new ScoreStats container format
                    using (var stream = File.OpenRead(SpeedRunFilePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(ScoreStats));
                        var stats = (ScoreStats)ser.ReadObject(stream);
                        if (stats?.SpeedRuns != null && stats.SpeedRuns.Count > 0)
                            return stats.SpeedRuns.Last();
                    }
                }
                catch
                {
                    // fall through to attempt old single-summary format
                }

                // Fallback: older files may have contained a single SpeedRunSummary
                try
                {
                    using (var stream = File.OpenRead(SpeedRunFilePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(SpeedRunSummary));
                        return (SpeedRunSummary)ser.ReadObject(stream);
                    }
                }
                catch
                {
                    // treat as no valid saved run
                    return null;
                }
            }
        }

        /// <summary>
        /// Delete persisted speed run stats (for example on Clear).
        /// </summary>
        public static void ClearLastSpeedRun()
        {
            lock (Sync)
            {
                if (File.Exists(SpeedRunFilePath))
                    File.Delete(SpeedRunFilePath);
            }
        }

        /// <summary>
        /// Loads all persisted speed runs (oldest -> newest). Returns empty list when none or on error.
        /// Supports both the newer ScoreStats container and the older single SpeedRunSummary format.
        /// </summary>
        public static IList<SpeedRunSummary> LoadAllSpeedRuns()
        {
            lock (Sync)
            {
                if (!File.Exists(SpeedRunFilePath))
                    return [];

                // Try new container format first
                try
                {
                    using (var stream = File.OpenRead(SpeedRunFilePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(ScoreStats));
                        var stats = (ScoreStats)ser.ReadObject(stream);
                        if (stats?.SpeedRuns != null)
                            return stats.SpeedRuns;
                    }
                }
                catch
                {
                    // fall through to attempt old single-summary format
                }

                // Fallback: older files may have contained a single SpeedRunSummary
                try
                {
                    using (var stream = File.OpenRead(SpeedRunFilePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(SpeedRunSummary));
                        var single = (SpeedRunSummary)ser.ReadObject(stream);
                        if (single != null)
                            return new List<SpeedRunSummary> { single };
                    }
                }
                catch
                {
                    // treat as none
                }

                return new List<SpeedRunSummary>();
            }
        }
    }

    /// <summary>
    /// Summary of a completed speed run.
    /// Times are persisted as milliseconds (TimesMs). Consumers can convert to TimeSpan.
    /// </summary>
    [DataContract]
    public class SpeedRunSummary
    {
        /// <summary>UTC timestamp when the run finished.</summary>
        [DataMember] public DateTimeOffset Timestamp { get; set; }

        /// <summary>Identifier of the last puzzle solved in the run (string for flexibility).</summary>
        [DataMember] public string LastPuzzleSolved { get; set; }

        /// <summary>List of per-puzzle solve times in milliseconds, in the order puzzles were solved.</summary>
        [DataMember] public List<long> TimesMs { get; set; } = new List<long>();

        /// <summary>List of per-puzzle press counts in the order puzzles were solved.</summary>
        [DataMember] public List<int> PressCounts { get; set; } = new List<int>();

        /// <summary>List of per-puzzle perfect flags (true when the solve used the minimal presses).</summary>
        [DataMember] public List<bool> IsPerfect { get; set; } = new List<bool>();

        /// <summary>Total elapsed time for the run in milliseconds (derived but stored for convenience).</summary>
        [DataMember] public long TotalElapsedMs { get; set; }

        /// <summary>Number of puzzles successfully solved in the run.</summary>
        [DataMember] public int SolvedCount { get; set; }

        // not serialized
        public IEnumerable<TimeSpan> Times => TimesMs?.Select(ms => TimeSpan.FromMilliseconds(ms)) ?? Enumerable.Empty<TimeSpan>();
    }
}