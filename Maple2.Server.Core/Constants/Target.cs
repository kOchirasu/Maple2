using System.Net;

namespace Maple2.Server.Core.Constants;

public static class Target {
    public const string GAME_DB_CONNECTION = "Server=localhost;Database=game-server;User=root;Password=maplestory";
    public const string DATA_DB_CONNECTION = "Server=localhost;Database=maple-data;User=root;Password=maplestory";
    
    public const string SEVER_NAME = "Paperwood";
    
    public static readonly IPAddress LOGIN_IP = IPAddress.Loopback;
    public const ushort LOGIN_PORT = 20001;

    public static readonly IPAddress GAME_IP = IPAddress.Loopback;
    public const ushort GAME_PORT = 22001;
    public const ushort GAME_CHANNEL = 1;

    public static readonly IPAddress GRPC_WORLD_IP = IPAddress.Loopback;
    public const ushort GRPC_WORLD_PORT = 20101;

    public static readonly IPAddress GRPC_CHANNEL_IP = IPAddress.Loopback;
    public const ushort GRPC_CHANNEL_PORT = 22101;
}
