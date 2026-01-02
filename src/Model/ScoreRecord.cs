using System;
using System.Runtime.Serialization;

namespace LightsOutCube.Model
{
    [DataContract]
    public class ScoreRecord
    {
        [DataMember] public DateTimeOffset Timestamp { get; set; }
        [DataMember] public TimeSpan Duration { get; set; }
        [DataMember] public int PressCount { get; set; }
        [DataMember] public bool IsPerfect { get; set; }
        [DataMember] public int PuzzleId { get; set; } // optional: seed or file name
        [DataMember] public string AppVersion { get; set; } // optional
    }
}