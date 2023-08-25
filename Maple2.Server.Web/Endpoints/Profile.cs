using System.IO;
using Maple2.Server.Web.Constants;
using Microsoft.AspNetCore.Http;

namespace Maple2.Server.Web.Endpoints;

public static class ProfileEndpoint {
    public static IResult Get(long characterId, string hash) {
        string fullPath = $"{Target.DataDir}/profiles/{characterId}/{hash}.png";
        if (!File.Exists(fullPath)) {
            return Results.BadRequest();
        }

        FileStream profileImage = File.OpenRead(fullPath);
        return Results.File(profileImage, contentType: "image/png");
    }
}
