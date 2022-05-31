using System;
using System.Web;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class InterfaceText : IByteSerializable {
    private readonly bool isLocalized;
    private readonly StringCode code;
    private readonly string[] args;
    private readonly string message;

    public InterfaceText(string message, bool htmlEncoded = false) {
        isLocalized = false;

        this.args = Array.Empty<string>();
        this.message = htmlEncoded ? message : HttpUtility.HtmlEncode(message);
    }

    public InterfaceText(StringCode code, params string[] args) {
        isLocalized = true;

        this.code = code;
        this.args = args;
        this.message = string.Empty;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteBool(isLocalized);
        writer.WriteInt(isLocalized ? 1 : 0); // Unknown
        if (isLocalized) {
            writer.Write<StringCode>(code);
            writer.WriteInt(args.Length);
            foreach (string arg in args) {
                writer.WriteUnicodeString(arg);
            }
        } else {
            writer.WriteUnicodeString(message);
        }
    }
}
