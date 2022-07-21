using System;
using System.Net;

namespace Maple2.Server.Core.Constants;

public static class Target {
    public const string SERVER_NAME = "Paperwood";
    public const string LOCALE = "NA";

    public static readonly IPAddress LoginIp = IPAddress.Loopback;
    public static readonly ushort LoginPort = 20001;

    public static readonly IPAddress GameIp = IPAddress.Loopback;
    public static readonly ushort GamePort = 20002;

    public static readonly ushort GrpcWorldPort = 21001;
    public static readonly ushort GrpcChannelPort = 21002;

    static Target() {
        if (IPAddress.TryParse(Environment.GetEnvironmentVariable("LOGIN_IP"), out IPAddress? loginIpAddress)) {
            LoginIp = loginIpAddress;
        }
        if (ushort.TryParse(Environment.GetEnvironmentVariable("LOGIN_PORT"), out ushort loginPortOverride)) {
            LoginPort = loginPortOverride;
        }

        if (IPAddress.TryParse(Environment.GetEnvironmentVariable("GAME_IP"), out IPAddress? gameIpAddress)) {
            GameIp = gameIpAddress;
        }
        if (ushort.TryParse(Environment.GetEnvironmentVariable("GAME_PORT"), out ushort gamePortOverride)) {
            GamePort = gamePortOverride;
        }

        if (ushort.TryParse(Environment.GetEnvironmentVariable("GRPC_WORLD_PORT"), out ushort grpcWorldPortOverride)) {
            GrpcWorldPort = grpcWorldPortOverride;
        }
        if (ushort.TryParse(Environment.GetEnvironmentVariable("GRPC_CHANNEL_PORT"), out ushort grpcChannelPortOverride)) {
            GrpcChannelPort = grpcChannelPortOverride;
        }
    }
}
