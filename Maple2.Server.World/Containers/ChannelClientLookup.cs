using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using Serilog;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Containers;

public class ChannelClientLookup {
    private record Entry(IPEndPoint Endpoint, ChannelClient Client, CancellationTokenSource Cancel);

    private readonly ConcurrentDictionary<int, Entry> channels;

    private readonly ILogger logger = Log.ForContext<ChannelClientLookup>();

    public int Count => channels.Count;

    public ChannelClientLookup() {
        channels = new ConcurrentDictionary<int, Entry>();
    }

    // TODO
    public static int RandomChannel() {
        return 1;
    }

    public bool Contains(int channel) {
        return channels.ContainsKey(channel);
    }

    public bool TryAdd(int channel, IPEndPoint endpoint, ChannelBase channelBase) {
        var client = new ChannelClient(channelBase);

        var cancel = new CancellationTokenSource();
        Task.Factory.StartNew(() => MonitorGameChannel(channelBase, channel, cancel.Token), cancel.Token);
        return channels.TryAdd(channel, new Entry(endpoint, client, cancel));
    }

    public bool Remove(int channel) {
        if (!channels.TryRemove(channel, out Entry? entry)) {
            return false;
        }

        entry.Cancel.Cancel();
        return true;
    }

    public bool TryGetClient(int channel, [NotNullWhen(true)] out ChannelClient? client) {
        if (!channels.TryGetValue(channel, out Entry? entry)) {
            client = null;
            return false;
        }

        client = entry.Client;
        return true;
    }

    public bool TryGetEndpoint(int channel, [NotNullWhen(true)] out IPEndPoint? endpoint) {
        if (!channels.TryGetValue(channel, out Entry? entry)) {
            endpoint = null;
            return false;
        }

        endpoint = entry.Endpoint;
        return true;
    }

    private async Task MonitorGameChannel(ChannelBase grpcChannel, int channel, CancellationToken cancellationToken) {
        logger.Information("Begin monitoring game channel: {Channel} on {EndPoint}", channel, grpcChannel.Target);

        try {
            var healthClient = new Health.HealthClient(grpcChannel);
            while (true) {
                await Task.Delay(10000, cancellationToken); // Perform HealthCheck every 10s
                HealthCheckResponse response = await healthClient.CheckAsync(new HealthCheckRequest(), cancellationToken: cancellationToken);
                switch (response.Status) {
                    case HealthCheckResponse.Types.ServingStatus.Serving:
                        continue;
                    case HealthCheckResponse.Types.ServingStatus.NotServing:
                        logger.Warning("Removing unhealthy game channel: {Channel}", channel);
                        Remove(channel);
                        return;
                }

                logger.Error("Unexpected status: {Status}", response.Status);
            }
        } catch (OperationCanceledException) {
            logger.Warning("Removing game channel: {Channel} due to cancellation", channel);
            Remove(channel);
        } catch (Exception ex) {
            logger.Warning("Removing game channel due to broken connection: {Channel}", channel);
            Remove(channel);
        }
    }
}
