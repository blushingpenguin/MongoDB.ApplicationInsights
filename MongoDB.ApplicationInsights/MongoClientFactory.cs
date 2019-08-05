using Microsoft.ApplicationInsights;
using MongoDB.Driver;

namespace MongoDB.ApplicationInsights
{
    /// <summary>
    /// Constructs mongo clients with application insights telemetry applied
    /// </summary>
    public class MongoClientFactory : IMongoClientFactory
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// The telemetry settings in use
        /// </summary>
        public MongoApplicationInsightsSettings Settings { get; }

        /// <summary>
        /// Construct a factory
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to send telemetry to</param>
        /// <param name="settings">Telemetry settings</param>
        public MongoClientFactory(
            TelemetryClient telemetryClient = null,
            MongoApplicationInsightsSettings settings = null)
        {
            _telemetryClient = telemetryClient;
            Settings = settings ?? new MongoApplicationInsightsSettings();
        }

        /// <summary>
        /// Construct a mongo client with telemetry applied
        /// </summary>
        /// <param name="clientSettings">The mongo client settings to use</param>
        /// <returns>The client</returns>
        public IMongoClient GetClient(MongoClientSettings clientSettings)
        {
            if (_telemetryClient != null)
            {
                new MongoApplicationInsightsTelemetry(
                    clientSettings, _telemetryClient, Settings);
            }
            return new MongoClient(clientSettings);
        }

        /// <summary>
        /// Construct a mongo client with telemetry applied
        /// </summary>
        /// <param name="connectionString">The connection string to use</param>
        /// <returns>The client</returns>
        public IMongoClient GetClient(string connectionString)
        {
            return GetClient(
                MongoClientSettings.FromConnectionString(connectionString));
        }

        /// <summary>
        /// Construct a mongo client with telemetry applied
        /// </summary>
        /// <param name="mongoUrl">The connection string to use</param>
        /// <returns>The client</returns>
        public IMongoClient GetClient(MongoUrl mongoUrl)
        {
            return GetClient(MongoClientSettings.FromUrl(mongoUrl));
        }
    }
}
