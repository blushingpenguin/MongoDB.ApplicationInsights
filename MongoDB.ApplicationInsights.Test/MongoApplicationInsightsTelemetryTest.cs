using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace MongoDB.ApplicationInsights.Test
{
    public class MongoApplicationInsightsTelemetryTest
    {
        class Mocks
        {
            public StubTelemetry StubTelemetry { get; }
            public ITelemetryChannel TelemetryChannel { get => StubTelemetry.TelemetryChannel; }
            public TelemetryClient TelemetryClient { get => StubTelemetry.TelemetryClient; }
            public MongoApplicationInsightsTelemetry Telemetry { get; }

            public Mocks(bool createTelemetry = true, MongoApplicationInsightsSettings settings = null)
            {
                StubTelemetry = new StubTelemetry();

                if (createTelemetry)
                {
                    var mongoClientSettings = MongoClientSettings.FromConnectionString(
                        "mongodb://localhost:27017/");
                    Telemetry = new MongoApplicationInsightsTelemetry(
                        mongoClientSettings,
                        TelemetryClient,
                        settings ?? new MongoApplicationInsightsSettings());
                }
            }

            public DependencyTelemetry GetSingleTelemetry()
            {
                TelemetryChannel.Received(1).Send(Arg.Any<ITelemetry>());
                return (DependencyTelemetry)TelemetryChannel
                        .ReceivedCalls()
                        .Where(c => c.GetMethodInfo().Name == "Send")
                        .Single()
                        .GetArguments()[0];
            }
        }

        [Test]
        public void NullClientSettingsThrows()
        {
            var mocks = new Mocks(false);
            Action a = () => new MongoApplicationInsightsTelemetry(
                null,
                mocks.TelemetryClient,
                new MongoApplicationInsightsSettings());
            a.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("clientSettings");
        }

        [Test]
        public void NullTelemetryClientThrows()
        {
            Action a = () => new MongoApplicationInsightsTelemetry(
                MongoClientSettings.FromConnectionString("mongodb://localhost:27017"),
                null,
                new MongoApplicationInsightsSettings());
            a.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("telemetryClient");
        }

        [Test]
        public void NullSettingsThrows()
        {
            var mocks = new Mocks(false);
            Action a = () => new MongoApplicationInsightsTelemetry(
                MongoClientSettings.FromConnectionString("mongodb://localhost:27017"),
                mocks.TelemetryClient,
                null);
            a.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("settings");
        }

        [Test]
        public void FormatUnspecifiedDnsEndPoint()
        {
            var result = MongoApplicationInsightsTelemetry.FormatEndPoint(
                new DnsEndPoint("test", 27017));
            result.Should().Be("test:27017");
        }

        [Test]
        public void FormatIPv4DnsEndPoint()
        {
            var result = MongoApplicationInsightsTelemetry.FormatEndPoint(
                new DnsEndPoint("test", 27017, AddressFamily.InterNetwork));
            result.Should().Be("test:27017");
        }

        [Test]
        public void FormatIPEndPoint()
        {
            var result = MongoApplicationInsightsTelemetry.FormatEndPoint(
                new IPEndPoint(new IPAddress(new byte[] { 0x7f, 0, 0, 1 }), 27017));
            result.Should().Be("127.0.0.1:27017");
        }

        private static readonly ConnectionId _connectionId = new ConnectionId(
            new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)));

        [Test]
        public void FilteredCommandStarted()
        {
            var mocks = new Mocks();
            mocks.Telemetry.OnCommandStarted(
                new CommandStartedEvent(
                    "saslStart", 
                    new BsonDocument(),
                    new DatabaseNamespace("test"), 
                    null, 
                    1, 
                    _connectionId));
            // Complete the command and check no telemetry was recorded
            mocks.Telemetry.OnCommandSucceeded(
                new CommandSucceededEvent("saslStart", new BsonDocument(), null, 
                    1, _connectionId, TimeSpan.FromSeconds(1)));
            mocks.TelemetryChannel.DidNotReceive().Send(Arg.Any<ITelemetry>());
        }

        private static CommandStartedEvent CreateFindStartedEvent(int requestId) =>
            new CommandStartedEvent(
                "find",
                BsonDocument.Parse("{find:{field:\"blah\"}}"),
                new DatabaseNamespace("test"),
                null,
                requestId,
                _connectionId);

        private static CommandSucceededEvent CreateFindSucceededEvent(int requestId) =>
            new CommandSucceededEvent("find", new BsonDocument(), null,
                    requestId, _connectionId, TimeSpan.FromSeconds(1));

        private static CommandFailedEvent CreateFindFailedEvent(int requestId) =>
            new CommandFailedEvent("find", new Exception("oh dear"), null,
                requestId, _connectionId, TimeSpan.FromSeconds(1));

        [Test]
        public void CommandSucceededNotStartedIgnored()
        {
            var mocks = new Mocks();
            mocks.Telemetry.OnCommandSucceeded(CreateFindSucceededEvent(2));
            mocks.TelemetryChannel.DidNotReceive().Send(Arg.Any<ITelemetry>());
        }

        [Test]
        public void CommandSucceeded()
        {
            var mocks = new Mocks();
            mocks.Telemetry.OnCommandStarted(CreateFindStartedEvent(2));
            // Complete the command and check telemetry was recorded
            mocks.Telemetry.OnCommandSucceeded(CreateFindSucceededEvent(2));
            var telemetry = mocks.GetSingleTelemetry();
            telemetry.Success.Should().BeTrue();
            telemetry.Data.Should().Be("{ \"find\" : { \"field\" : \"blah\" } }");
        }

        [Test]
        public void CommandFailedNotStartedIgnored()
        {
            var mocks = new Mocks();
            mocks.Telemetry.OnCommandFailed(CreateFindFailedEvent(3));
            mocks.TelemetryChannel.DidNotReceive().Send(Arg.Any<ITelemetry>());
        }

        [Test]
        public void CommandFailed()
        {
            var mocks = new Mocks();
            mocks.Telemetry.OnCommandStarted(CreateFindStartedEvent(3));
            // Complete the command and check telemetry was recorded
            mocks.Telemetry.OnCommandFailed(CreateFindFailedEvent(3));
            var telemetry = mocks.GetSingleTelemetry();
            telemetry.Properties["Exception"].Should().Be("System.Exception: oh dear");
            telemetry.Data.Should().Be("{ \"find\" : { \"field\" : \"blah\" } }");
        }

        [Test]
        public void CommandWithActivity()
        {
            var activity = new Activity("test");
            activity.AddBaggage("bag", "cup");
            activity.Start();

            try
            {
                var mocks = new Mocks();
                mocks.Telemetry.OnCommandStarted(CreateFindStartedEvent(4));
                // Complete the command and check telemetry was recorded
                mocks.Telemetry.OnCommandSucceeded(CreateFindSucceededEvent(4));
                var telemetry = mocks.GetSingleTelemetry();
                telemetry.Success.Should().BeTrue();
                telemetry.Data.Should().Be("{ \"find\" : { \"field\" : \"blah\" } }");
                telemetry.Context.Operation.Id.Should().Be(activity.RootId);
                telemetry.Context.Operation.ParentId.Should().Be(activity.Id);
            }
            finally
            {
                activity.Stop();
            }
        }

        [Test]
        public void CommandWithActivityAndBaggage()
        {
            var activity = new Activity("test");
            activity.AddBaggage("bag", "cup");
            activity.Start();

            try
            {
                var mocks = new Mocks();
                mocks.Telemetry.OnCommandStarted(CreateFindStartedEvent(4));
                // Complete the command and check telemetry was recorded
                mocks.Telemetry.OnCommandSucceeded(CreateFindSucceededEvent(4));
                var telemetry = mocks.GetSingleTelemetry();
                telemetry.Success.Should().BeTrue();
                telemetry.Data.Should().Be("{ \"find\" : { \"field\" : \"blah\" } }");
                telemetry.Context.Operation.Id.Should().Be(activity.RootId);
                telemetry.Context.Operation.ParentId.Should().Be(activity.Id);
                telemetry.Context.GlobalProperties["bag"].Should().Be("cup");
            }
            finally
            {
                activity.Stop();
            }
        }

        [Test]
        public void PruneExpiresOldEntries()
        {
            var mocks = new Mocks(true, 
                new MongoApplicationInsightsSettings { MaxQueryTime = TimeSpan.FromHours(1) });
            mocks.Telemetry.OnCommandStarted(CreateFindStartedEvent(1));
            mocks.Telemetry.OnCommandStarted(CreateFindStartedEvent(2));
            mocks.Telemetry.Prune(DateTime.UtcNow.AddHours(2)); // expire
            mocks.Telemetry.OnCommandSucceeded(CreateFindSucceededEvent(1));
            mocks.Telemetry.OnCommandSucceeded(CreateFindSucceededEvent(2));
            mocks.TelemetryChannel.DidNotReceive().Send(Arg.Any<ITelemetry>());
        }
    }
}
