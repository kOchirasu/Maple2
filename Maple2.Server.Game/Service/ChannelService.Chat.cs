using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Model.Game;
using Maple2.Server.Channel.Service;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Service;

public partial class ChannelService {
    public override Task<ChatResponse> Chat(ChatRequest request, ServerCallContext context) {
        switch (request.ChatCase) {
            case ChatRequest.ChatOneofCase.Whisper:
                WhisperChat(request);
                return Task.FromResult(new ChatResponse());
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

    private void WhisperChat(ChatRequest request) {
        if (server.GetSession(request.Whisper.RecipientId, out GameSession? session)) {
            session.Send(ChatPacket.Whisper(
                request.AccountId, request.CharacterId, request.Name, request.Message,string.Empty));
        }

        throw new RpcException(new Status(StatusCode.NotFound, $"Unable to whisper: {request.Whisper.RecipientName}"));
    }
}
