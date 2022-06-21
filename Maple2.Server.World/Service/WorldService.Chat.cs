using System.Threading.Tasks;
using Grpc.Core;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    public override Task<ChatResponse> Chat(ChatRequest request, ServerCallContext context) {
        switch (request.ChatCase) {
            case ChatRequest.ChatOneofCase.Whisper: {
                return WhisperChat(request);
            }
            case ChatRequest.ChatOneofCase.Party:
                return Task.FromResult(new ChatResponse());
            case ChatRequest.ChatOneofCase.Guild:
                return Task.FromResult(new ChatResponse());
            case ChatRequest.ChatOneofCase.World:
                return Task.FromResult(new ChatResponse());
            case ChatRequest.ChatOneofCase.Super:
                return Task.FromResult(new ChatResponse());
            case ChatRequest.ChatOneofCase.Club:
                return Task.FromResult(new ChatResponse());
            case ChatRequest.ChatOneofCase.Wedding:
                return Task.FromResult(new ChatResponse());
            default:
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, $"Invalid chat type: {request.ChatCase}"));
        }
    }

    private Task<ChatResponse> WhisperChat(ChatRequest request) {
        // Ideally we would know which channel a character was on.
        if (!playerChannels.TryGetValue(request.CharacterId, out int channel)) {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Unable to whisper: {request.Whisper.RecipientName}"));
        }

        if (!channels.TryGetValue(channel, out ChannelClient? channelClient)) {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"Unable to whisper: {request.Whisper.RecipientName} on channel: {channel}"));
        }

        var channelChat = new ChatRequest {
            AccountId = request.AccountId,
            CharacterId = request.CharacterId,
            Name = request.Name,
            Message = request.Message,
            Whisper = new ChatRequest.Types.Whisper {
                RecipientId = request.Whisper.RecipientId,
                RecipientName = request.Whisper.RecipientName,
            }
        };

        try {
            return Task.FromResult(channelClient.Chat(channelChat));
        } catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound) {
            playerChannels.TryRemove(request.CharacterId, out _);
            return Task.FromResult(new ChatResponse());
        }
    }
}
