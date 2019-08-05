using FluentAssertions;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace MongoDB.ApplicationInsights.Test
{
    public class MongoClientFactoryTest
    {
        MongoClientFactory CreateFactory() =>
            new MongoClientFactory(new StubTelemetry().TelemetryClient);

        [Test]
        public void ConstructorWithoutSettings()
        {
            var factory = CreateFactory();
            factory.Settings.Should().BeEquivalentTo(new MongoApplicationInsightsSettings());
        }

        [Test]
        public void ConstructorWithSettings()
        {
            var stub = new StubTelemetry();
            var settings = new MongoApplicationInsightsSettings
            {
                FilteredCommands = new HashSet<string>(),
                MaxQueryTime = TimeSpan.FromDays(6)
            };
            var factory = new MongoClientFactory(stub.TelemetryClient, settings);
            factory.Settings.Should().BeEquivalentTo(settings);
        }

        [Test]
        public void GetClientWithSettings()
        {
            var factory = CreateFactory();
            var result = factory.GetClient(
                MongoClientSettings.FromConnectionString("mongodb://localhost"));
            result.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27017));
        }

        [Test]
        public void GetClientWithConnectionString()
        {
            var factory = CreateFactory();
            var result = factory.GetClient("mongodb://localhost:27018");
            result.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27018));
        }

        [Test]
        public void GetClientWithMongoUrl()
        {
            var factory = CreateFactory();
            var result = factory.GetClient(new MongoUrl("mongodb://localhost:27019"));
            result.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27019));
        }

        [Test]
        public void GetClientWithNoTelemetry()
        {
            var factory = new MongoClientFactory();
            var result = factory.GetClient("mongodb://localhost");
            result.Settings.Server.Should().BeEquivalentTo(
                new MongoServerAddress("localhost", 27017));
        }
    }
}
