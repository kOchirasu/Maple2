using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Maple2.Server.Core.Constants;
using Serilog;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Containers;

public class ChannelClientLookup {
#if DEBUG
    private static readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(1);
#else
    private static readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(5);
#endif

    private record Entry(IPEndPoint Endpoint, ChannelClient Client, Health.HealthClient Health);

    private readonly Entry[] channels;
    private readonly bool[] activeChannels;

    private readonly ILogger logger = Log.ForContext<ChannelClientLookup>();

    public int Count => channels.Length;

    public IEnumerable<int> Keys {
        get {
            for (int i = 0; i < activeChannels.Length; i++) {
                if (activeChannels[i]) {
                    yield return i + 1;
                }
            }
        }
    }

    // TODO: Dynamic channel
    public ChannelClientLookup(int channelCount = 2) {
        channels = new Entry[channelCount];
        activeChannels = new bool[channelCount];

        string[]? channelServices = Environment.GetEnvironmentVariable("CHANNEL_SERVICE")?.Split(",");
        if (channelServices == null) {
            channelServices = new string[channelCount];
            Array.Fill(channelServices, IPAddress.Loopback.ToString());
        }
        for (int i = 0; i < channelCount; i++) {
            var gameEndpoint = new IPEndPoint(Target.GameIp, Target.GamePort + i);
            var grpcUri = new Uri($"http://{channelServices[i]}:{Target.GrpcChannelPort + i}");
            GrpcChannel grpcChannel = GrpcChannel.ForAddress(grpcUri);
            var client = new ChannelClient(grpcChannel);
            var healthClient = new Health.HealthClient(grpcChannel);
            channels[i] = new Entry(gameEndpoint, client, healthClient);
        }

        var cancel = new CancellationToken();
        MonitorChannels(cancel);
    }

    public int FirstChannel() {
        for (int i = 0; i < activeChannels.Length; i++) {
            if (activeChannels[i]) {
                return i + 1;
            }
        }

        return 0;
    }

    public bool Contains(int channel) {
        return ValidChannel(channel) && activeChannels[channel - 1];
    }

    public bool ValidChannel(int channel) {
        return channel > 0 && channel <= activeChannels.Length;
    }

    public bool TryGetClient(int channel, [NotNullWhen(true)] out ChannelClient? client) {
        if (!ValidChannel(channel)) {
            client = null;
            return false;
        }

        client = channels[channel - 1].Client;
        return true;
    }

    public bool TryGetActiveEndpoint(int channel, [NotNullWhen(true)] out IPEndPoint? endpoint) {
        int i = channel - 1;
        if (!ValidChannel(channel) || !activeChannels[i]) {
            endpoint = null;
            return false;
        }

        endpoint = channels[i].Endpoint;
        return true;
    }

    private void MonitorChannels(CancellationToken cancellationToken) {
        for (int i = 0; i < channels.Length; i++) {
            int channel = i + 1;
            Task.Factory.StartNew(() => MonitorChannel(channel, cancellationToken), cancellationToken: cancellationToken);
        }

        // Try to let channel list populate, this only happens once.
        Thread.Sleep(1000);
    }

    private async Task MonitorChannel(int channel, CancellationToken cancellationToken) {
        int i = channel - 1;
        logger.Information("Begin monitoring game channel: {Channel} for {EndPoint}", channel, channels[i].Endpoint);
        do {
            try {
                HealthCheckResponse response = await channels[i].Health.CheckAsync(new HealthCheckRequest(), cancellationToken: cancellationToken);
                switch (response.Status) {
                    case HealthCheckResponse.Types.ServingStatus.Serving:
                        if (!activeChannels[i]) {
                            logger.Information("Channel {Channel} has become active", channel);
                            activeChannels[i] = true;
                        }
                        break;
                    default:
                        if (activeChannels[i]) {
                            logger.Information("Channel {Channel} has become inactive due to {Status}", channel, response.Status);
                            activeChannels[i] = false;
                        }
                        break;
                }
            } catch (RpcException ex) {
                if (ex.Status.StatusCode != StatusCode.Unavailable) {
                    logger.Warning("{Error} monitoring channel {Channel}", ex.Message, channel);
                }
                activeChannels[i] = false;
            }

            await Task.Delay(MonitorInterval, cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }
}
