# Changelog

This changelog is for the EventStoreDB client. For the KurrentDB client release notes, please visit: https://github.com/kurrent-io/KurrentDB-Client-Dotnet/releases

All notable changes to this project will be documented in this file.

## [23.3.0] - 2024-07-24

### Added
- DEV-303 - Support providing an x.509 certificate for user authentication [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/295)

### Changed
- Update bouncy castle to 2.3.1 [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/306)

### Fixed
- Fix StreamType/EventType filtering cases [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/309)
- Fix tracing injection when event is non-JSON [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/317)
- Fix check for parent context when extract propagation context fails [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/319)
- Fix channel factory to use root ca if provided [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/325)
- fix: unnecessary http message handler invocation [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/344)

### Other
- DEV-297 - Add tracing instrumentation of Append & Subscribe operations [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/294)
- ESDB-147-8 - Move to Cloudsmith docker registry [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/288)
- DEV-45-2 - Pull es-gencert-cli from Cloudsmith [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/298)
- DEV-47 - Retain callback-based subscription APIs alongside AsyncEnumerable APIs [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/297)
- Add OTel and User certificates samples [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/307)
- Add missing endregion marker in samples [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/308)
- DB-29-22 - Make CI Test Action More Configurable [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/300)
- Tracing improvements [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/311)
- Bump System.Text.Json from 8.0.3 to 8.0.4 in /src/EventStore.Client [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/315)
- Bumped assembly copyright range end from 2020 to current year [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/318)
- Account for nullable events [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/324)
- Add github automated release configuration [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/327)
- Account for standard ports when collecting tags [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/328)
- Clarify server side filtering sample and align with docs [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/330)
- Docs for legacy v23.3 [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/359)

## [23.2.0] - 2024-05-04

### Added
- .Net 8.0 Support [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/275)
- .NET 4.8 support [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/267)
- Add certificate in http fallback handler [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/289)
- Subscriptions Without Callbacks (DEV-112) [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/282)
- DEV-92 - Add missing option tlsCAFile [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/281)
- Add filtering to non-subscription reads of $all [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/278)

### Fixed
- DEV-125 - Fix partial append on error append to stream [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/283)
- DEV-266 - Return null for discovery mode or multiple hosts in connection string [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/280)
- Decode Base64 usernames/passwords as UTF-8 strings [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/269)

### Other
- Read semantic versions when reading server version [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/287)
- Update CI badges [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/262)

## [23.1.0] - 2023-08-22

### Added
- Support authenticated gossip read request (DB-305) [View](https://github.com/kurrent-io/KurrentDB-Client-Dotnet/pull/253)

## [23.0.0] - 2023-02-08

### Breaking Changes
- Removed `Timeout` from `EventStoreOperationOptions` and moved it to an explicit `deadline` parameter on all operations except for subscriptions. Consequently, `configureOperationOptions` callback has been removed for most operations. [EventStore-Client-Dotnet#194](https://github.com/EventStore/EventStore-Client-Dotnet/pull/194)
- Drop support for `netcoreapp3.1` [EventStore-Client-Dotnet#204](https://github.com/EventStore/EventStore-Client-Dotnet/pull/204)

### Added
- Allow channels to open extra connections if they reach the max streams per connection limit (i.e. too may concurrent grpc calls- 100 by default) [EventStore-Client-Dotnet#218](https://github.com/EventStore/EventStore-Client-Dotnet/pull/218)
- Correct the error message when deleting a stream using gRPC. [EventStore-Client-Dotnet#221](https://github.com/EventStore/EventStore-Client-Dotnet/pull/221)
- Support `List()` over gRPC for listing persistent subscriptions [EventStore-Client-Dotnet#180](https://github.com/EventStore/EventStore-Client-Dotnet/pull/180)
- Support `ReplayParked()` over gRPC for replaying parked messages [EventStore-Client-Dotnet#180](https://github.com/EventStore/EventStore-Client-Dotnet/pull/180)
- Support `GetInfo()` over gRPC for returning details of a persistent subscription [EventStore-Client-Dotnet#180](https://github.com/EventStore/EventStore-Client-Dotnet/pull/180)
- Target `net6.0` [EventStore-Client-Dotnet#204](https://github.com/EventStore/EventStore-Client-Dotnet/pull/204)
- Target `net7.0` [EventStore-Client-Dotnet#230](https://github.com/EventStore/EventStore-Client-Dotnet/pull/230)

### Fixed
- Incorrect error message when deleting a stream using gRPC [EventStore-Client-Dotnet#221](https://github.com/EventStore/EventStore-Client-Dotnet/pull/221) 
- Dispose the gRPC call underlying a Read if the read is only partially consumed [EventStore-Client-Dotnet#234](https://github.com/EventStore/EventStore-Client-Dotnet/pull/234)
- Support `RestartSubsystem()` over gRPC for restarting the persistent subscription subsystem [EventStore-Client-Dotnet#180](https://github.com/EventStore/EventStore-Client-Dotnet/pull/180)
- Remove the exception that logs an error when the subscription is cancelled [EventStore-Client-Dotnet#209](https://github.com/EventStore/EventStore-Client-Dotnet/pull/209)

## [22.0.0]

### Breaking Changes
- Remove autoAck from Persistent Subscriptions [EventStore-Client-DotNet#175](https://github.com/EventStore/EventStore-Client-Dotnet/pull/175)
- Adjustments to Disposal [EventStore-Client-DotNet#189](https://github.com/EventStore/EventStore-Client-Dotnet/pull/189)
- Rename SoftDeleteAsync to DeleteAsync [EventStore-Client-DotNet#197](https://github.com/EventStore/EventStore-Client-Dotnet/pull/197)
- Standardize gRPC Client Deadlines [EventStore-Client-DotNet#194](https://github.com/EventStore/EventStore-Client-Dotnet/pull/194)

### Fixed
- Get Certifications Path More Reliably [EventStore-Client-DotNet#178](https://github.com/EventStore/EventStore-Client-Dotnet/pull/178)
- Make Client More Backwards Compatibility Friendly [EventStore-Client-DotNet#125](https://github.com/EventStore/EventStore-Client-Dotnet/pull/125)
- Send correct writeCheckpoint option when disabling/aborting a projection [EventStore-Client-DotNet#116](https://github.com/EventStore/EventStore-Client-Dotnet/pull/116)
- Force Rediscovery Only when Lost Connection [EventStore-Client-DotNet#195](https://github.com/EventStore/EventStore-Client-Dotnet/pull/195)
- Align Persistent Subscription Names [EventStore-Client-DotNet#198](https://github.com/EventStore/EventStore-Client-Dotnet/pull/198)
- Trigger rediscovery when failing to send a message on a streaming call [EventStore-Client-DotNet#222](https://github.com/EventStore/EventStore-Client-Dotnet/pull/222)

### Added
- Introduce New Types For Subscription Positions [EventStore-Client-DotNet#188](https://github.com/EventStore/EventStore-Client-Dotnet/pull/188)
- Detect Server Capabilities [EventStore-Client-DotNet#172](https://github.com/EventStore/EventStore-Client-Dotnet/pull/172)
- Implement Last/Next StreamPosition/Position [EventStore-Client-DotNet#151](https://github.com/EventStore/EventStore-Client-Dotnet/pull/151)
- Add filtered persistent subscriptions [EventStore-Client-DotNet#122](https://github.com/EventStore/EventStore-Client-Dotnet/pull/122)
- Implement persistent subscriptions to $all: [EventStore-Client-DotNet#108](https://github.com/EventStore/EventStore-Client-Dotnet/pull/108)
- Implement parameterless IComparable for StreamPosition and StreamRevision [EventStore-Client-DotNet#111](https://github.com/EventStore/EventStore-Client-Dotnet/pull/111)

### Changed
- send 'requires-leader' header based on NodePreference [EventStore-Client-DotNet#131](https://github.com/EventStore/EventStore-Client-Dotnet/pull/131)

## [21.2.0] - 2021-02-22

### Fixed
- Fix Default Keep Alive [EventStore-Client-DotNet#107](https://github.com/EventStore/EventStore-Client-Dotnet/pull/107)
- Check Disposal Before Invoking CheckpointReached [EventStore-Client-DotNet#105](https://github.com/EventStore/EventStore-Client-Dotnet/pull/105)
- Fixed Enumerator Exception Being Overridden w/ DeadlineExceeded [EventStore-Client-DotNet#100](https://github.com/EventStore/EventStore-Client-Dotnet/pull/100)

### Changed
- Use Grpc.Core for netcoreapp3.1 and net48 [EventStore-Client-DotNet#93](https://github.com/EventStore/EventStore-Client-Dotnet/pull/93)

## [20.10.0] - 2020-12-09

### Breaking Changes
- Increase gRPC Deadline to Infinite on Persistent Subscriptions [EventStore-Client-DotNet#84](https://github.com/EventStore/EventStore-Client-Dotnet/pull/84)

### Added
- Add Support for Single DNS Gossip Seed [EventStore-Client-DotNet#91](https://github.com/EventStore/EventStore-Client-Dotnet/pull/91)
- Add Connection String Overloads for DI Extensions [EventStore-Client-DotNet#83](https://github.com/EventStore/EventStore-Client-Dotnet/pull/83)
- Add projection reset to client [EventStore-Client-DotNet#79](https://github.com/EventStore/EventStore-Client-Dotnet/pull/79)

## [20.6.1] - 2020-09-30

### Breaking Changes
- WrongExpectedVersionResult / WrongExpectedVersionException will use values from server [EventStore-Client-DotNet#73](https://github.com/EventStore/EventStore-Client-Dotnet/pull/73)
- Convert from StreamPosition to StreamRevision; StreamRevision on IWriteResult [EventStore-Client-DotNet#53](https://github.com/EventStore/EventStore-Client-Dotnet/pull/53)

### Added
- Add restarting persistent subscriptions [EventStore-Client-DotNet#68](https://github.com/EventStore/EventStore-Client-Dotnet/pull/68)
- Implement connection string [EventStore-Client-DotNet#49](https://github.com/EventStore/EventStore-Client-Dotnet/pull/49)
- Add GossipOverHttps option to EventStoreClientConnectivitySettings [EventStore-Client-DotNet#51](https://github.com/EventStore/EventStore-Client-Dotnet/pull/51)
- Add Service Collection Extensions to all Clients [EventStore-Client-DotNet#45](https://github.com/EventStore/EventStore-Client-Dotnet/pull/45)
- Add ChannelCredentials to EventStoreClientSettings [EventStore-Client-DotNet#46](https://github.com/EventStore/EventStore-Client-Dotnet/pull/46)

### Changed
- Use gRPC Auth Pipeline Instead of Metadata [EventStore-Client-DotNet#52](https://github.com/EventStore/EventStore-Client-Dotnet/pull/52)

## [20.6.0] - 2020-06-09

### Breaking Changes
- Rename HttpEndPointIp to HttpEndPointAddress [EventStore-Client-DotNet#32](https://github.com/EventStore/EventStore-Client-Dotnet/pull/32)

### Added
- Support infinite timeouts [EventStore-Client-DotNet#30](https://github.com/EventStore/EventStore-Client-Dotnet/pull/30)
- Do gossip requests over gRPC [EventStore-Client-DotNet#27](https://github.com/EventStore/EventStore-Client-Dotnet/pull/27)
- Support Bearer Tokens in User Credentials [EventStore-Client-DotNet#24](https://github.com/EventStore/EventStore-Client-Dotnet/pull/24)

### Changed
- Restructured stream name for future planned changes [EventStore-Client-DotNet#33](https://github.com/EventStore/EventStore-Client-Dotnet/pull/33)

## [20.6.0-rc] - 2020-06-15

- Initial Release
