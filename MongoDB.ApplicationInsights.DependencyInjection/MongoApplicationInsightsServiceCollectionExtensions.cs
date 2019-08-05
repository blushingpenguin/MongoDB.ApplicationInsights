using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;

namespace MongoDB.ApplicationInsights.DependencyInjection
{
    /// <summary>
    /// Helpers for configuration mongo with application insights telemetry
    /// </summary>
    public static class MongoApplicationInsightsServiceCollectionExtensions
    {
        /// <summary>
        /// Add an IMongoClientFactory instance configured with the given settings
        /// </summary>
        /// <param name="services">The services container</param>
        /// <param name="settings">The telemetry settings to use</param>
        /// <returns>The services container</returns>
        public static IServiceCollection AddMongoClientFactory(
            this IServiceCollection services,
            MongoApplicationInsightsSettings settings = null
        ) => services
                .AddSingleton(settings ?? new MongoApplicationInsightsSettings())
                .AddSingleton<IMongoClientFactory>(sp => new MongoClientFactory(
                    sp.GetService<TelemetryClient>(),
                    sp.GetRequiredService<MongoApplicationInsightsSettings>()
                ));

        private static IMongoClientFactory GetFactory(IServiceProvider sp) =>
            sp.GetRequiredService<IMongoClientFactory>();

        /// <summary>
        /// Add an IMongoClient instance configured with the given settings
        /// </summary>
        /// <param name="services">The services container</param>
        /// <param name="connectionString">The connection string to use</param>
        /// <param name="settings">The telemetry settings to use</param>
        /// <returns>The services container</returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection services,
            string connectionString,
            MongoApplicationInsightsSettings settings = null
        ) => services
                .AddMongoClientFactory(settings)
                .AddSingleton(sp => GetFactory(sp).GetClient(connectionString));

        /// <summary>
        /// Add an IMongoClient instance configured with the given settings
        /// </summary>
        /// <param name="services">The services container</param>
        /// <param name="url">The connection string to use</param>
        /// <param name="settings">The telemetry settings to use</param>
        /// <returns>The services container</returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection services,
            MongoUrl url,
            MongoApplicationInsightsSettings settings = null
        ) => services
                .AddMongoClientFactory(settings)
                .AddSingleton(sp => GetFactory(sp).GetClient(url));

        /// <summary>
        /// Add an IMongoClient instance configured with the given settings
        /// </summary>
        /// <param name="services">The services container</param>
        /// <param name="clientSettings">The mongo client settings to use</param>
        /// <param name="settings">The telemetry settings to use</param>
        /// <returns>The services container</returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection services,
            MongoClientSettings clientSettings,
            MongoApplicationInsightsSettings settings = null
        ) => services
                .AddMongoClientFactory(settings)
                .AddSingleton(sp => GetFactory(sp).GetClient(clientSettings));
    }
}
