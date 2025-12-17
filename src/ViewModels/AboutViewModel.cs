using System;
using System.Collections.ObjectModel;
using System.Linq;
using LightsOutCube.Model;
using System.Reflection;

namespace LightsOutCube.ViewModels
{
    public class AboutViewModel
    {
        public ObservableCollection<SpeedRunEntryWrapper> SpeedRuns { get; } = [];
        public ObservableCollection<HighScoreEntry> HighScores { get; } = [];
        string _version = "";
        public string Version
        {
            get
            {
                if (!string.IsNullOrEmpty(_version))
                    return _version;
                
                try
                {
                    var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    string version = null;
                    var infoAttr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    if (infoAttr != null && !string.IsNullOrWhiteSpace(infoAttr.InformationalVersion))
                        version = infoAttr.InformationalVersion;
                    if (string.IsNullOrEmpty(version))
                    {
                        var fileAttr = asm.GetCustomAttribute<AssemblyFileVersionAttribute>();
                        if (fileAttr != null && !string.IsNullOrWhiteSpace(fileAttr.Version))
                            version = fileAttr.Version;
                    }
                    if (string.IsNullOrEmpty(version))
                        version = asm.GetName().Version?.ToString() ?? "n/a";
                    _version = version;
                }
                catch
                {
                    _version = "n/a";
                }
                return _version;
            }
        }

        public void Refresh()
        {
            SpeedRuns.Clear();

            var runs = ScoreStore.LoadAllSpeedRuns() ?? Enumerable.Empty<SpeedRunSummary>();
            foreach (var run in runs.OrderByDescending(r => r.Timestamp))
            {
                var times = run.TimesMs?.ToList() ?? new System.Collections.Generic.List<long>();
                var pressCounts = run.PressCounts ?? new System.Collections.Generic.List<int>();
                var perfects = run.IsPerfect ?? new System.Collections.Generic.List<bool>();

                var wrapper = new SpeedRunEntryWrapper
                {
                    SolvedCount = run.SolvedCount,
                    TotalTime = FormatDuration(TimeSpan.FromMilliseconds(run.TotalElapsedMs)),
                    LastPuzzle = run.LastPuzzleSolved,
                    Timestamp = run.Timestamp.LocalDateTime.ToString("g")
                };

                for (int i = 0; i < times.Count; i++)
                {
                    // insert header as first item when i == 0
                    if (i == 0 && wrapper.PuzzleTimes.Count == 0)
                    {
                        wrapper.PuzzleTimes.Add(new PuzzleTimeEntry
                        {
                            Index = 0,
                            Time = "Time",
                            PressCount = 0,
                            PerfectText = "Perfect",
                            IsHeader = true
                        });
                    }

                    var t = TimeSpan.FromMilliseconds(times[i]);
                    var press = pressCounts.ElementAtOrDefault(i);
                    var isPerf = perfects.ElementAtOrDefault(i);
                    wrapper.PuzzleTimes.Add(new PuzzleTimeEntry
                    {
                        Index = i + 1,
                        Time = FormatDuration(t),
                        PressCount = press,
                        PerfectText = isPerf ? "Yes" : "No"
                    });
                }

                SpeedRuns.Add(wrapper);
            }
        }

        public void RefreshHighScores()
        {
            HighScores.Clear();

            var records = ScoreStore.LoadAll() ?? Enumerable.Empty<ScoreRecord>();
            foreach (var rec in records.OrderByDescending(r => r.Timestamp))
            {
                HighScores.Add(new HighScoreEntry
                {
                    Puzzle = rec.PuzzleId ?? string.Empty,
                    Time = FormatDuration(rec.Duration),
                    PressCount = rec.PressCount,
                    PerfectText = rec.IsPerfect ? "Yes" : "No",
                    Timestamp = rec.Timestamp.LocalDateTime.ToString("g")
                });
            }
        }

        private static string FormatDuration(TimeSpan ts)
        {
            // Format as minutes:seconds (e.g. 2:05)
            var minutes = (int)ts.TotalMinutes;
            var seconds = ts.Seconds;
            return $"{minutes}:{seconds:D2}";
        }
    }

    public class SpeedRunEntryWrapper
    {
        public int SolvedCount { get; set; }
        public string TotalTime { get; set; }
        public string LastPuzzle { get; set; }
        public string Timestamp { get; set; }
        public System.Collections.Generic.List<PuzzleTimeEntry> PuzzleTimes { get; set; } = new System.Collections.Generic.List<PuzzleTimeEntry>();
    }

    public class PuzzleTimeEntry
    {
        public int Index { get; set; }
        public string Time { get; set; }
        public int PressCount { get; set; }
        public string PerfectText { get; set; }
        // When true this entry represents the header row for a run's puzzle list
        public bool IsHeader { get; set; }
    }

    public class HighScoreEntry
    {
        public string Puzzle { get; set; }
        public string Time { get; set; }
        public int PressCount { get; set; }
        public string PerfectText { get; set; }
        public string Timestamp { get; set; }
    }
}
