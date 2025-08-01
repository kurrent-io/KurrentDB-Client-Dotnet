#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

// ReSharper disable CheckNamespace

using Kurrent.Client.Registry;
using Kurrent.Client.Schema;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using Contracts = EventStore.Client.Streams;

namespace Kurrent.Client.Model;

static class StreamsClientV1Mapper {
    static readonly Contracts.ReadReq.Types.Options.Types.ControlOption       DefaultControlOption       = new() { Compatibility = 1 };
    static readonly Contracts.ReadReq.Types.Options.Types.SubscriptionOptions DefaultSubscriptionOptions = new();
    static readonly Contracts.ReadReq.Types.Options.Types.UUIDOption          DefaultUuidOptions         = new();
    static readonly EventStore.Client.Empty                                   DefaultEmpty               = new();

    public static class Requests {
        public static Contracts.ReadReq CreateSubscriptionRequest(AllSubscriptionOptions options) {
            return new() { Options = new() {
                ReadDirection = Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                Subscription  = DefaultSubscriptionOptions,
                UuidOption    = DefaultUuidOptions,
                NoFilter      = DefaultEmpty,
                All           = ConvertToAllOptions(options.Start),
                Filter        = ConvertToFilterOptions(options.Filter, options.Heartbeat)
            }};
        }

        public static Contracts.ReadReq CreateStreamSubscriptionRequest(StreamSubscriptionOptions options) {
            return new() { Options = new() {
                ReadDirection = Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                Subscription  = DefaultSubscriptionOptions,
                UuidOption    = DefaultUuidOptions,
                NoFilter      = DefaultEmpty,
                Stream        = ConvertToStreamOptions(options.Stream, options.Start)
            }};
        }

        public static Contracts.ReadReq CreateReadRequest(ReadAllOptions options) {
            return new Contracts.ReadReq {
                Options = new() {
                    UuidOption    = DefaultUuidOptions,
                    ControlOption = DefaultControlOption,
                    ReadDirection = options.Direction switch {
                        ReadDirection.Backwards => Contracts.ReadReq.Types.Options.Types.ReadDirection.Backwards,
                        ReadDirection.Forwards  => Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                    },
                    All = new() { Position = new() {
                        CommitPosition  = options.Start,
                        PreparePosition = options.Start
                    } },
                    Count  = (ulong)options.Limit,
                    Filter = ConvertToFilterOptions(options.Filter, options.Heartbeat)
                }
            };
        }

        /// <summary>
        ///  Creates a request to read a single record from the log at the specified position, resolving links if necessary.
        /// </summary>
        public static Contracts.ReadReq CreateInspectRecordRequest(LogPosition position) {
            return new Contracts.ReadReq {
                Options = new() {
                    UuidOption    = DefaultUuidOptions,
                    ControlOption = DefaultControlOption,
                    Count         = 1,
                    ResolveLinks  = true,
                    ReadDirection = Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                    All = new() { Position = new() {
                        CommitPosition  = position,
                        PreparePosition = position
                    } }
                }
            };
        }

        public static Contracts.ReadReq CreateReadRequest(ReadStreamOptions options) {
            return new Contracts.ReadReq {
                Options = new() {
                    UuidOption    = DefaultUuidOptions,
                    ControlOption = DefaultControlOption,
                    NoFilter      = DefaultEmpty,
                    ReadDirection = options.Direction switch {
                        ReadDirection.Backwards => Contracts.ReadReq.Types.Options.Types.ReadDirection.Backwards,
                        ReadDirection.Forwards  => Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                    },
                    Stream = ConvertToStreamOptions(options.Stream, options.Start),
                    Count  = (ulong)options.Limit,
                }
            };
        }

        // NoStream     = -1
        // Any          = -2
        // StreamExists = -4

        public static Contracts.DeleteReq CreateDeleteRequest(StreamName stream, StreamRevision revision) {
            var req = new Contracts.DeleteReq { Options = new() { StreamIdentifier = stream.Value } };
            return revision == StreamRevision.Unset
                ? req.With(x => x.Options.StreamExists = DefaultEmpty)
                : req.With(x => x.Options.Revision = (ulong)revision.Value);
        }

        public static Contracts.TombstoneReq CreateTombstoneRequest(StreamName stream, StreamRevision revision) {
            var req = new Contracts.TombstoneReq { Options = new() { StreamIdentifier = stream.Value } };
            return revision == StreamRevision.Unset
                ? req.With(x => x.Options.StreamExists = DefaultEmpty)
                : req.With(x => x.Options.Revision = (ulong)revision.Value);
        }

        // public static Contracts.DeleteReq CreateDeleteRequest(StreamName stream, ExpectedStreamState expectedState) {
        //     var req = new Contracts.DeleteReq { Options = new() { StreamIdentifier = stream.Value } };
        //     return expectedState switch {
        //         _ when expectedState == ExpectedStreamState.Any          => req.With(x => x.Options.Any = DefaultEmpty),
        //         _ when expectedState == ExpectedStreamState.NoStream     => req.With(x => x.Options.NoStream = DefaultEmpty),
        //         _ when expectedState == ExpectedStreamState.StreamExists => req.With(x => x.Options.StreamExists = DefaultEmpty),
        //         _                                                        => req.With(x => x.Options.Revision = expectedState)
        //     };
        // }

        // public static Contracts.TombstoneReq CreateTombstoneRequest(StreamName stream, ExpectedStreamState expectedState) {
        //     var req = new Contracts.TombstoneReq { Options = new() { StreamIdentifier = stream.Value } };
        //     return expectedState switch {
        //         _ when expectedState == ExpectedStreamState.Any          => req.With(x => x.Options.Any = DefaultEmpty),
        //         _ when expectedState == ExpectedStreamState.NoStream     => req.With(x => x.Options.NoStream = DefaultEmpty),
        //         _ when expectedState == ExpectedStreamState.StreamExists => req.With(x => x.Options.StreamExists = DefaultEmpty),
        //         _                                                        => req.With(x => x.Options.Revision = expectedState)
        //     };
        // }

        static Contracts.ReadReq.Types.Options.Types.FilterOptions? ConvertToFilterOptions(ReadFilter filter, HeartbeatOptions heartbeat) {
            if (filter == ReadFilter.None)
                return null;

            var options = filter.Scope switch {
                ReadFilterScope.Stream => new Contracts.ReadReq.Types.Options.Types.FilterOptions {
                    StreamIdentifier = new() { Regex = filter.Expression }
                },
                ReadFilterScope.Record => new Contracts.ReadReq.Types.Options.Types.FilterOptions {
                    EventType = new() { Regex = filter.Expression }
                },
            };

            // what is this?!
            //options.Count = DefaultEmpty;
            // if (filter.MaxSearchWindow.HasValue)
            //     options.Max = (uint)heartbeatRecordsThreshold;
            // else
            //     options.Count = DefaultEmpty;

            options.Max                          = (uint)heartbeat.RecordsThreshold;
            options.CheckpointIntervalMultiplier = 1;

            return options;
        }

        static Contracts.ReadReq.Types.Options.Types.AllOptions ConvertToAllOptions(LogPosition start) =>
            start switch {
                _ when start == LogPosition.Latest   => new() { End      = DefaultEmpty },
                _ when start == LogPosition.Earliest => new() { Start    = DefaultEmpty },
                _                                    => new() { Position = new() { CommitPosition = start, PreparePosition = start } }
            };

        static Contracts.ReadReq.Types.Options.Types.StreamOptions ConvertToStreamOptions(string stream, StreamRevision start) =>
            start switch {
                _ when start == StreamRevision.Max => new() { StreamIdentifier = stream, End      = DefaultEmpty },
                _ when start == StreamRevision.Min => new() { StreamIdentifier = stream, Start    = DefaultEmpty },
                _                                  => new() { StreamIdentifier = stream, Revision = (ulong)start.Value }
            };
    }

    public static Heartbeat MapToHeartbeat(this Contracts.ReadResp.Types.Checkpoint checkpoint) {
        var position  = LogPosition.From((long)checkpoint.CommitPosition);
        var timestamp = checkpoint.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateCheckpoint(position, timestamp);
    }

    public static Heartbeat MapToHeartbeat(this Contracts.ReadResp.Types.CaughtUp caughtUp) {
        var position  = caughtUp.Position is not null ? LogPosition.From((long)caughtUp.Position.CommitPosition) : LogPosition.Unset;
        var revision  = caughtUp.HasStreamRevision ? StreamRevision.From(caughtUp.StreamRevision) : StreamRevision.Unset;
        var timestamp = caughtUp.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateCaughtUp(position, revision, timestamp);
    }

    public static Heartbeat MapToHeartbeat(this Contracts.ReadResp.Types.FellBehind fellBehind) {
        var position  = fellBehind.Position is not null ? LogPosition.From((long)fellBehind.Position.CommitPosition) : LogPosition.Unset;
        var revision  = fellBehind.HasStreamRevision ? StreamRevision.From(fellBehind.StreamRevision) : StreamRevision.Unset;
        var timestamp = fellBehind.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateFellBehind(position, revision, timestamp);
    }

    public static LogPosition MapToLogPosition(this Contracts.DeleteResp.Types.Position? position) =>
        position switch {
            null => LogPosition.Unset,
            _    => LogPosition.From((long)position.CommitPosition)
        };

    public static LogPosition MapToLogPosition(this Contracts.TombstoneResp.Types.Position? position) =>
        position switch {
            null => LogPosition.Unset,
            _    => LogPosition.From((long)position.CommitPosition)
        };

 //    public static async ValueTask<Record> MapToRecord(
	// 	this Contracts.ReadResp.Types.ReadEvent.Types.RecordedEvent recordedEvent,
	// 	ISchemaSerializerProvider serializerProvider,
	// 	IMetadataDecoder metadataDecoder,
	// 	SchemaRegistryPolicy registryPolicy,
 //        bool skipDecoding = false,
	// 	CancellationToken ct = default
	// ) {
	// 	// first parse all info
	// 	var stream           = StreamName.From(recordedEvent.StreamIdentifier!);
	// 	var recordId         = Uuid.FromDto(recordedEvent.Id).ToGuid();
	// 	var revision         = StreamRevision.From((long)recordedEvent.StreamRevision);
	// 	var position         = LogPosition.From((long)recordedEvent.CommitPosition);
	// 	var schemaName       = SchemaName.From(recordedEvent.Metadata[Constants.Metadata.Type]);
	// 	var schemaDataFormat = recordedEvent.Metadata[Constants.Metadata.ContentType].GetSchemaDataFormat(); // it will always be json or octet-stream
	// 	var data             = recordedEvent.Data.Memory;
	// 	var rawMetadata      = recordedEvent.CustomMetadata.Memory;
	// 	var timestamp        = Convert.ToInt64(recordedEvent.Metadata[Constants.Metadata.Created]).FromTicksSinceEpoch();
	// 	var metadata         = metadataDecoder.Decode(rawMetadata, new(stream, schemaName, schemaDataFormat));
 //
 //        // create a decoder
 //        IRecordDecoder decoder = new RecordDecoder(serializerProvider, registryPolicy);
 //
	// 	// and we are done
 //        var record = new Record(decoder) {
 //            Id             = recordId,
 //            Stream         = stream,
 //            StreamRevision = revision,
 //            Position       = position,
 //            Timestamp      = timestamp,
 //            Metadata       = metadata,
 //            Data           = data
 //        };
 //
 //        // now decode the record if required
 //        if (!skipDecoding)
 //            await record.TryDecode(ct);
 //
 //        return record;
	// }

    // {
    //     "$v": "3:-1:1:4",
    //     "$c": 12961,
    //     "$p": 12961,
    //     "$o": "TicTacToe-bad03a1c87c6",
    //     "$causedBy": "0197d630-9297-7000-9806-85795845c414"
    // }

    public static async ValueTask<Record> MapToRecord(
        this Contracts.ReadResp.Types.ReadEvent readEvent,
        ISchemaSerializerProvider serializerProvider,
        IMetadataDecoder metadataDecoder,
        SchemaRegistryPolicy registryPolicy,
        bool skipDecoding = false,
        CancellationToken ct = default
    ) {
        var recordedEvent = readEvent.Event;

        // first parse all info
        var stream           = StreamName.From(recordedEvent.StreamIdentifier!);
        var recordId         = Uuid.FromDto(recordedEvent.Id).ToGuid();
        var revision         = StreamRevision.From((long)recordedEvent.StreamRevision);
        var position         = LogPosition.From((long)recordedEvent.CommitPosition);
        var schemaName       = SchemaName.From(recordedEvent.Metadata[Constants.Metadata.Type]);
        var schemaDataFormat = recordedEvent.Metadata[Constants.Metadata.ContentType].GetSchemaDataFormat(); // it will always be json or octet-stream
        var data             = recordedEvent.Data.Memory;
        var rawMetadata      = recordedEvent.CustomMetadata.Memory;
        var timestamp        = Convert.ToInt64(recordedEvent.Metadata[Constants.Metadata.Created]).FromTicksSinceEpoch();

        var metadata = metadataDecoder.Decode(rawMetadata, new(stream, schemaName, schemaDataFormat));

        var link = readEvent.Link is { } linkEvent
            ? new Link {
                Name     = linkEvent.Metadata[Constants.Metadata.Type],
                Position = (long)linkEvent.CommitPosition,
                Revision = (long)linkEvent.StreamRevision,
                Stream   = StreamName.From(linkEvent.StreamIdentifier!),
            }
            : Link.None;

        var indexRevision = readEvent.HasCommitPosition
            ? StreamRevision.From((long)readEvent.CommitPosition)
            : link.Revision == StreamRevision.Unset ? revision : link.Revision;

        // create a decoder
        IRecordDecoder decoder = new RecordDecoder(serializerProvider, registryPolicy);

        // and we are done
        var record = new Record(decoder) {
            Id             = recordId,
            Stream         = stream,
            StreamRevision = revision,
            Position       = position,
            Timestamp      = timestamp,
            Metadata       = metadata,
            Data           = data,
            Link           = link,
            IndexRevision  = indexRevision
        };

        // now decode the record if required
        if (!skipDecoding)
            await record.TryDecode(ct);

        return record;
    }

    public static ValueTask<Record> MapToRecord(
        this Contracts.ReadResp.Types.ReadEvent readEvent,
        ISchemaSerializerProvider serializerProvider,
        IMetadataDecoder metadataDecoder,
        bool skipDecoding = false,
        CancellationToken ct = default
    ) => MapToRecord(readEvent, serializerProvider, metadataDecoder, SchemaRegistryPolicy.NoRequirements, skipDecoding, ct);
}
