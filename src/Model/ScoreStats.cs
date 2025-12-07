using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LightsOutCube.Model
{
    /// <summary>
    /// Container for aggregated score statistics persisted to disk.
    /// Holds a list of completed speed run summaries (oldest -> newest).
    /// </summary>
    [DataContract]
    public class ScoreStats
    {
        [DataMember]
        public List<SpeedRunSummary> SpeedRuns { get; set; } = new List<SpeedRunSummary>();
    }
}