using Grpc.Core;

namespace KurrentDB.Client;

public record ChannelInfo(ChannelBase Channel, ServerCapabilities ServerCapabilities, CallInvoker CallInvoker);
