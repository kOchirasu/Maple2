using System;
using System.IO;

namespace Maple2.Server.Web.Constants;

public static class Target {
    private static readonly string SolutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    public static readonly string DataDir = Path.Combine(SolutionDir, "Maple2.Server.Web/Data");
}
