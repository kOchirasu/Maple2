﻿using System;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class CurrencyPacket {
    public static ByteWriter UpdateMeso(Currency currency) {
        var pWriter = Packet.Of(SendOp.MESO);
        pWriter.WriteLong(currency.Meso);
        pWriter.WriteInt(); // PCBang related

        return pWriter;
    }

    public static ByteWriter UpdateMeret(Currency currency, long delta) {
        var pWriter = Packet.Of(SendOp.MERET);
        pWriter.WriteLong(currency.Meret);
        pWriter.WriteLong(); // extra meret
        pWriter.WriteLong(currency.GameMeret);
        pWriter.WriteLong(); // extra game meret
        pWriter.WriteLong(delta);

        return pWriter;
    }

    public static ByteWriter UpdateCurrency(Currency currency, CurrencyType type, long delta, long overflow) {
        var pWriter = Packet.Of(SendOp.CURRENCY_TOKEN);
        pWriter.Write<CurrencyType>(type);
        switch (type) {
            case CurrencyType.ValorToken:
                pWriter.WriteLong(currency.ValorToken);
                break;
            case CurrencyType.Treva:
                break;
            case CurrencyType.Rue:
                break;
            case CurrencyType.HaviFruit:
                break;
            case CurrencyType.ReverseCoin:
                break;
            case CurrencyType.MentorToken:
                break;
            case CurrencyType.MenteeToken:
                break;
            case CurrencyType.StarPoint:
                break;
            case CurrencyType.MesoToken:
                break;
        }

        pWriter.WriteLong(delta);
        pWriter.WriteInt(); // Unknown
        pWriter.WriteLong(overflow);
        return pWriter;
    }
}