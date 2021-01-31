using Maple2.Server.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Servers.Login {
    public class LoginSession : Session {
        protected override PatchType Type => PatchType.Delete;

        public LoginSession(ILogger<LoginSession> logger) : base(logger) { }
    }
}