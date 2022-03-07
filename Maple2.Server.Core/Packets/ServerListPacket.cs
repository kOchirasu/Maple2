using System.Collections.Generic;
using System.Net;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets; 

public static class ServerListPacket {
    public static ByteWriter SetServers(string serverName, IList<IPEndPoint> serverIps, ushort channels) {
        var pWriter = Packet.Of(SendOp.SERVER_LIST);
        pWriter.WriteByte(0x01); // If 0 -> s_login_err_connect
        pWriter.WriteInt(1); // Unknown
        pWriter.WriteUnicodeString(serverName);
        pWriter.WriteByte(4); // IPv4?
        pWriter.Write<ushort>((ushort)serverIps.Count);
        foreach (IPEndPoint endpoint in serverIps) {
            pWriter.WriteUnicodeString(endpoint.Address.ToString());
            pWriter.Write<ushort>((ushort)endpoint.Port);
        }
        pWriter.WriteInt(100); // Const
        
        // Channels
        pWriter.Write<ushort>(channels);
        // I think this should be sorted by population
        for (short i = 1; i <= channels; i++) {
            pWriter.WriteShort(i);
        }

        return pWriter;
    }
}
