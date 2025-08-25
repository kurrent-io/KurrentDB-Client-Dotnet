using Grpc.Core;
using Grpc.Net.Client;

namespace KurrentDB.Client;

record ChannelInfo(ChannelBase Channel, GrpcChannelOptions Options, ServerCapabilities ServerCapabilities, CallInvoker CallInvoker);
