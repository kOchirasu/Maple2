using System.CommandLine;
using System.CommandLine.IO;
using System.Net;
using System.Text;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class GameConsole : IConsole {
    public IStandardStreamWriter Out { get; }
    public bool IsOutputRedirected => true;
    public IStandardStreamWriter Error { get; }
    public bool IsErrorRedirected => true;
    public bool IsInputRedirected => true;

    public GameConsole(GameSession session) {
        Error = new GameErrorStreamWriter(session);
        Out = new GameOutputStreamWriter(session);
    }

    private struct GameOutputStreamWriter : IStandardStreamWriter {
        private readonly GameSession session;
        private readonly StringBuilder pending;
        private bool joinNewline;

        public GameOutputStreamWriter(GameSession session) {
            this.session = session;
            this.pending = new StringBuilder();
            this.joinNewline = false;
        }

        public void Write(string? value) {
            if (value == null) {
                return;
            }

            if (value.EndsWith('\r')) {
                return;
            } else if (value.EndsWith('\n')) {
                value = value.TrimEnd('\r', '\n');
                pending.Append(WebUtility.HtmlEncode(value));
                if (joinNewline) {
                    joinNewline = false;
                    return;
                }
            } else {
                if (value.EndsWith(':')) {
                    pending.Append($"<b>{WebUtility.HtmlEncode(value)}</b>");
                    if (value is "Description:" or "Usage:") {
                        joinNewline = true;
                    }
                } else {
                    pending.Append(WebUtility.HtmlEncode(value));
                }
                return;
            }

            string result = pending.ToString();
            if (!string.IsNullOrWhiteSpace(result)) {
                session.Send(ChatPacket.Alert(result, true));
            }

            pending.Clear();
        }
    }

    private readonly struct GameErrorStreamWriter : IStandardStreamWriter {
        private readonly GameSession session;

        public GameErrorStreamWriter(GameSession session) {
            this.session = session;
        }

        public void Write(string? value) {
            // if (value != null) {
            //     value = value.TrimEnd('\r', '\n', ' ');
            //     session.Send(ChatPacket.System("ERROR", value));
            // }
        }
    }
}
