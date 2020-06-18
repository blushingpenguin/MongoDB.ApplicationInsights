using System;
using System.Collections.Generic;

namespace MongoDB.ApplicationInsights
{
    public class MongoApplicationInsightsSettings
    {
        /// <summary>
        /// Mongo commands which will be ignored
        /// </summary>
        public HashSet<string> FilteredCommands { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "buildInfo",
                "getLastError",
                "isMaster",
                "ping",
                "saslStart",
                "saslContinue"
            };

        /// <summary>
        /// The maximum length of time a query may run for before telemetry tracking is discarded
        /// This is to prevent memory leaks if the MongoDB driver reports that a query has been
        /// started, but not whether it has succeeded or failed. If you have queries that run for
        /// longer than the default time (4 hours), then you will need to increase this value
        /// to obtain telemetry for them.
        /// </summary>
        public TimeSpan MaxQueryTime { get; set; } = new TimeSpan(4, 0, 0);

        /// <summary>
        /// Gets or sets a value indicating whether to track the Mongo command text in MongoDB dependencies.
        /// </summary>
        public bool EnableMongoCommandTextInstrumentation { get; set; } = true;
    }
}
