using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Maple2.PacketLib.Crypto;
using Maple2.PacketLib.Tools;
using Maple2.Server.Constants;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Network; 

public enum PatchType : byte {
    Delete = 0,
    Patch = 1,
    Ignore = 2,
}

public abstract class Session : IDisposable {
    public const uint VERSION = 12;
    private const uint BLOCK_IV = 12; // TODO: should this be variable

    private const int HANDSHAKE_SIZE = 19;
    private const int STOP_TIMEOUT = 2000;

    public EventHandler<string> OnError;
    public EventHandler<PoolByteReader> OnPacket;

    private bool disposed;
    private uint siv;
    private uint riv;

    private string name;
    private TcpClient client;
    private NetworkStream networkStream;
    private MapleCipher.Encryptor sendCipher;
    private MapleCipher.Decryptor recvCipher;

    private readonly Thread thread;
    private readonly QueuedPipeScheduler pipeScheduler;
    private readonly Pipe recvPipe;

    protected abstract PatchType Type { get; }
    protected readonly ILogger logger;

    private static readonly RNGCryptoServiceProvider rng = new();

    protected Session(ILogger logger) {
        this.logger = logger;

        thread = new Thread(StartInternal);
        pipeScheduler = new QueuedPipeScheduler();
        var options = new PipeOptions(
            readerScheduler: pipeScheduler,
            writerScheduler: pipeScheduler,
            useSynchronizationContext: false
        );
        recvPipe = new Pipe(options);
    }

    public void Init([NotNull] TcpClient client) {
        if (disposed) {
            throw new ObjectDisposedException("Session has been disposed.");
        }

        // Allow client to close immediately
        client.LingerState = new LingerOption(true, 0);
        name = client.Client.RemoteEndPoint?.ToString();

        byte[] sivBytes = new byte[4];
        byte[] rivBytes = new byte[4];
        rng.GetBytes(sivBytes);
        rng.GetBytes(rivBytes);
        this.siv = BitConverter.ToUInt32(sivBytes);
        this.riv = BitConverter.ToUInt32(rivBytes);

        this.client = client;
        this.networkStream = client.GetStream();
        this.sendCipher = new MapleCipher.Encryptor(VERSION, siv, BLOCK_IV);
        this.recvCipher = new MapleCipher.Decryptor(VERSION, riv, BLOCK_IV);
    }

    public virtual void Dispose() {
        if (disposed) return;

        disposed = true;
        Complete();
        thread.Join(STOP_TIMEOUT);

        CloseClient();
    }

    protected void Complete() {
        recvPipe.Writer.Complete();
        recvPipe.Reader.Complete();
        pipeScheduler.Complete();
    }

    public void Disconnect() {
        if (disposed) return;

        logger.LogInformation($"Disconnected {this}");
        Dispose();
    }

    public bool Connected() {
        if (disposed || client?.Client == null) {
            return false;
        }

        Socket socket = client.Client;
        return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
    }

    public void Start() {
        if (disposed) {
            throw new ObjectDisposedException("Session has been disposed.");
        }
        if (client == null) {
            throw new InvalidOperationException("Cannot start a session without a client.");
        }

        thread.Start();
    }

    public void Send(params byte[] packet) => SendInternal(packet, packet.Length);

    public void Send(ByteWriter packet) => SendInternal(packet.Buffer, packet.Length);

    public override string ToString() => $"{GetType().Name} from {name}";


    private void StartInternal() {
        try {
            PerformHandshake(); // Perform handshake to initialize connection

            // Pipeline tasks can be run asynchronously
            Task writeTask = WriteRecvPipe(client.Client, recvPipe.Writer);
            Task readTask = ReadRecvPipe(recvPipe.Reader);
            Task.WhenAll(writeTask, readTask).ContinueWith(_ => CloseClient());

            while (!disposed && pipeScheduler.OutputAvailableAsync().Result) {
                pipeScheduler.ProcessQueue();
            }
        } catch (Exception ex) {
            if (!disposed) {
                logger.LogError("Exception on session thread: ", ex);
            }
        } finally {
            Disconnect();
        }
    }

    private void PerformHandshake() {
        var handshake = new ByteWriter(HANDSHAKE_SIZE);
        handshake.Write<ushort>(SendOp.REQUEST_VERSION);
        handshake.Write<uint>(VERSION);
        handshake.Write<uint>(riv);
        handshake.Write<uint>(siv);
        handshake.Write<uint>(BLOCK_IV);
        handshake.WriteByte((byte)Type);

        // No encryption for handshake
        using PoolByteWriter packet = sendCipher.WriteHeader(handshake.Buffer, 0, handshake.Length);
        logger.LogDebug($"Handshake: {packet}");
        SendRaw(packet);
    }

    private async Task WriteRecvPipe(Socket socket, PipeWriter writer) {
        try {
            FlushResult result;
            do {
                Memory<byte> memory = writer.GetMemory();
                int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                if (bytesRead <= 0) {
                    break;
                }

                writer.Advance(bytesRead);

                result = await writer.FlushAsync();
            } while (!disposed && !result.IsCompleted);
        } catch (Exception) {
            Disconnect();
        }
    }

    private async Task ReadRecvPipe(PipeReader reader) {
        try {
            ReadResult result;
            do {
                result = await reader.ReadAsync();

                int bytesRead;
                ReadOnlySequence<byte> buffer = result.Buffer;
                while ((bytesRead = recvCipher.TryDecrypt(buffer, out PoolByteReader packet)) > 0) {
                    try {
                        LogRecv(packet);
                        OnPacket?.Invoke(this, packet); // handle packet
                    } finally {
                        packet.Dispose();
                    }
                    buffer = buffer.Slice(bytesRead);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            } while (!disposed && !result.IsCompleted);
        } catch (Exception ex) {
            if (!disposed) {
                logger.LogError("Exception reading recv packet: ", ex);
            }
        } finally {
            Disconnect();
        }
    }

    private void SendInternal(byte[] packet, int length) {
        if (disposed) return;

        LogSend(packet, length);
        lock (sendCipher) {
            using PoolByteWriter encryptedPacket = sendCipher.Encrypt(packet, 0, length);
            SendRaw(encryptedPacket);
        }
    }

    private void SendRaw(ByteWriter packet) {
        if (disposed) return;

        try {
            networkStream.Write(packet.Buffer, 0, packet.Length);
        } catch (Exception) {
            Disconnect();
        }
    }

    private void CloseClient() {
        // Must close socket before network stream to prevent lingering
        client?.Client.Close();
        client?.Close();
    }

    private void LogSend(byte[] packet, int length) {
        // Filtering sync from logs
        short opcode = (short) (packet[1] << 8 | packet[0]);
        if (opcode != 0x1C && opcode != 0x59 && opcode != 0x80 && opcode != 0x11) {
            logger.LogTrace($"SEND ({length}): {packet.ToHexString(length, ' ')}");
        }
    }

    private void LogRecv(PoolByteReader packet) {
        short opcode = (short) (packet.Buffer[1] << 8 | packet.Buffer[0]);
        if (opcode != 0x12 && opcode != 0x0B && opcode != 0x35) {
            // Filtering sync from logs
            logger.LogTrace($"RECV ({packet.Length}): {packet}");
        }
    }
}