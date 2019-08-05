using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace MongoDB.ApplicationInsights.DependencyInjection.Test
{
    public class MongoApplicationInsightsServiceCollectionExtensionsTest
    {
        private IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            var config = new TelemetryConfiguration
            {
                TelemetryChannel = Substitute.For<ITelemetryChannel>(),
                InstrumentationKey = Guid.NewGuid().ToString(),
            };
            services.AddSingleton(new TelemetryClient(config));
            return services;
        }

        [Test]
        public void AddMongoClientFactoryWithoutSettings()
        {
            var services = CreateServices();
            services.AddMongoClientFactory();
            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IMongoClientFactory>();
            factory.Settings.Should().BeEquivalentTo(
                new MongoApplicationInsightsSettings());
        }

        [Test]
        public void AddMongoClientFactoryWithSettings()
        {
            var services = CreateServices();
            var settings = new MongoApplicationInsightsSettings
            {
                MaxQueryTime = TimeSpan.FromMinutes(10),
                FilteredCommands = new HashSet<string>()
            };
            services.AddMongoClientFactory(settings);
            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IMongoClientFactory>();
            factory.Settings.Should().BeEquivalentTo(settings);
        }

        [Test]
        public void AddMongoClientWithConnectionString()
        {
            var services = CreateServices();
            services.AddMongoClient("mongodb://localhost:27018");
            var sp = services.BuildServiceProvider();
            var client = sp.GetRequiredService<IMongoClient>();
            client.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27018));
        }

        [Test]
        public void AddMongoClientWithUrl()
        {
            var services = CreateServices();
            services.AddMongoClient(new MongoUrl("mongodb://testdb:27017/"));
            var sp = services.BuildServiceProvider();
            var client = sp.GetRequiredService<IMongoClient>();
            client.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("testdb", 27017));
        }

        [Test]
        public void AddMongoClientWithMongoSettings()
        {
            var services = CreateServices();
            var clientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost");
            services.AddMongoClient(clientSettings);
            var sp = services.BuildServiceProvider();
            var client = sp.GetRequiredService<IMongoClient>();
            client.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27017));
        }

        [Test]
        public void AddMongoClientWithNoTelemetry()
        {
            var services = new ServiceCollection();
            services.AddMongoClient("mongodb://localhost:27018");
            var sp = services.BuildServiceProvider();
            var client = sp.GetRequiredService<IMongoClient>();
            client.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27018));
        }
    }
}
