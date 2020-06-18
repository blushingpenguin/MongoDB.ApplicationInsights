﻿using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace MongoDB.ApplicationInsights
{
    struct CachedQuery
    {
        public DateTime CachedAt { get; set; }
        public DependencyTelemetry Telemetry { get; set; }
    }

    internal class MongoApplicationInsightsTelemetry
    {
        // Ideally we'd use a ConditionalWeakTable here, but there are no actual objects that
        // we can track the lifetime of in the callbacks from mongo. Instead, the time the
        // query is cached at is marked, and then periodically a scan is done (triggered
        // by a call) to prune any old entries. This is just to catch the case where we get
        // a started event for a request, but no success / failure callback.
        private readonly ConcurrentDictionary<int, CachedQuery> _queryCache =
            new ConcurrentDictionary<int, CachedQuery>();
        private readonly TelemetryClient _telemetryClient;
        private readonly MongoApplicationInsightsSettings _settings;
        private DateTime _nextPruneTime;

        public MongoApplicationInsightsTelemetry(
            MongoClientSettings clientSettings, 
            TelemetryClient telemetryClient,
            MongoApplicationInsightsSettings settings
        )
        {
            if (clientSettings == null)
            {
                throw new ArgumentNullException(nameof(clientSettings));
            }
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            clientSettings.ClusterConfigurator = clusterConfigurator =>
            {
                clusterConfigurator.Subscribe<CommandStartedEvent>(OnCommandStarted);
                clusterConfigurator.Subscribe<CommandSucceededEvent>(OnCommandSucceeded);
                clusterConfigurator.Subscribe<CommandFailedEvent>(OnCommandFailed);
            };
            _nextPruneTime = DateTime.UtcNow.Add(_settings.MaxQueryTime);
        }

        internal void Prune(DateTime now)
        {
            if (now < _nextPruneTime)
            {
                return;
            }
            var expiryTime = now.Subtract(_settings.MaxQueryTime);

            foreach (var cacheEntry in _queryCache)
            {
                if (cacheEntry.Value.CachedAt < expiryTime)
                {
                    _queryCache.TryRemove(cacheEntry.Key, out _);
                }
            }
            _nextPruneTime = now.Add(_settings.MaxQueryTime);
        }

        internal static string FormatEndPoint(EndPoint endPoint)
        {
            if (endPoint is DnsEndPoint dnsEndPoint)
            {
                return $"{dnsEndPoint.Host}:{dnsEndPoint.Port}";
            }
            return endPoint.ToString();
        }

        internal void OnCommandStarted(CommandStartedEvent evt)
        {
            Prune(DateTime.UtcNow);

            if (_settings.FilteredCommands.Contains(evt.CommandName))
            {
                return;
            }

            var target = $"{FormatEndPoint(evt.ConnectionId.ServerId.EndPoint)} | {evt.DatabaseNamespace}";
            var dependencyName = $"{target} | {evt.CommandName}";

            var commandText = string.Empty;
            if (_settings.EnableMongoCommandTextInstrumentation)
            {
                // Command can't be null -- the CommandStartedEvent constructor throws to prevent this
                commandText = evt.Command.ToString();
            }

            var telemetry = new DependencyTelemetry()
            {
                Name = dependencyName,
                Type = "MongoDB",
                Target = target,
                Data = commandText,
                Success = true,
            };
            telemetry.GenerateOperationId();
            telemetry.Timestamp = DateTimeOffset.UtcNow;

            /*
             * copying implementation below from Microsoft's SqlClientDiagnosticSourceListener
             * https://github.com/microsoft/ApplicationInsights-dotnet/blob/5ac6bb98d04b7da6d151c0338efece4c124c750a/WEB/Src/DependencyCollector/DependencyCollector/Implementation/SqlClientDiagnostics/SqlClientDiagnosticSourceListener.cs#L347
             */

            var activity = Activity.Current;

            if (activity != null)
            {
                // for web applications the IdFormat is W3C so without the below check the parient ID is set incorrectly
                if (activity.IdFormat == ActivityIdFormat.W3C)
                {
                    var traceId = activity.TraceId.ToHexString();
                    telemetry.Context.Operation.Id = traceId;
                    telemetry.Context.Operation.ParentId = activity.SpanId.ToHexString();
                }
                else
                {
                    telemetry.Context.Operation.Id = activity.RootId;
                    telemetry.Context.Operation.ParentId = activity.Id;
                }

                foreach (var item in activity.Baggage)
                {
                    if (!telemetry.Properties.ContainsKey(item.Key))
                    {
                        telemetry.Properties[item.Key] = item.Value;
                    }
                }
            }
            else
            {
                telemetry.Context.Operation.Id = telemetry.Id;
            }

            var query = new CachedQuery { CachedAt = DateTime.UtcNow, Telemetry = telemetry };
            _queryCache.TryAdd(evt.RequestId, query);
        }

        internal void OnCommandSucceeded(CommandSucceededEvent evt)
        {
            if (!_queryCache.TryRemove(evt.RequestId, out CachedQuery query))
            {
                return;
            }
            query.Telemetry.Duration = evt.Duration;
            _telemetryClient.TrackDependency(query.Telemetry);
        }

        internal void OnCommandFailed(CommandFailedEvent evt)
        {
            if (!_queryCache.TryRemove(evt.RequestId, out CachedQuery query))
            {
                return;
            }
            var telemetry = query.Telemetry;
            telemetry.Success = false;
            telemetry.Properties["Exception"] = evt.Failure.ToInvariantString();
            query.Telemetry.Duration = evt.Duration;
            _telemetryClient.TrackDependency(query.Telemetry);
        }
    }
}
