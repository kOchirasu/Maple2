using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Maple2.Server.Web.Endpoints;

[Route("/data/profiles")]
public class ProfileEndpoint : ControllerBase {
    private static readonly string SolutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    private static readonly string RootDir = Path.Combine(SolutionDir, "Maple2.Server.Web/Data");

    [HttpGet("avatar/{characterId:long}/{hash}.png")]
    public IResult Get(long characterId, string hash) {
        string fullPath = $"{RootDir}/profiles/{characterId}/{hash}.png";
        if (!System.IO.File.Exists(fullPath)) {
            return Results.BadRequest();
        }

        FileStream profileImage = System.IO.File.OpenRead(fullPath);
        return Results.File(profileImage, contentType: "image/png");
    }
}
