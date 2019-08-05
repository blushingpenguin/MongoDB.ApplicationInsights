using MongoDB.Driver;

namespace MongoDB.ApplicationInsights
{
    /// <summary>
    /// Constructs mongo clients with application insights telemetry applied
    /// </summary>
    public interface IMongoClientFactory
    {
        /// <summary>
        /// The telemetry settings in use
        /// </summary>
        MongoApplicationInsightsSettings Settings { get; }

        /// <summary>
        /// Construct a factory
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to send telemetry to</param>
        /// <param name="settings">Telemetry settings</param>
        IMongoClient GetClient(MongoClientSettings clientSettings);

        /// <summary>
        /// Construct a mongo client with telemetry applied
        /// </summary>
        /// <param name="clientSettings">The mongo client settings to use</param>
        /// <returns>The client</returns>
        IMongoClient GetClient(MongoUrl mongoUrl);

        /// <summary>
        /// Construct a mongo client with telemetry applied
        /// </summary>
        /// <param name="connectionString">The connection string to use</param>
        /// <returns>The client</returns>
        IMongoClient GetClient(string connectionString);
    }
}
