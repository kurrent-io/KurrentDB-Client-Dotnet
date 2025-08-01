using Grpc.Core;

namespace KurrentDB.Client;

record ChannelInfo(ChannelBase Channel, ServerCapabilities ServerCapabilities, CallInvoker CallInvoker);
