using System;
using System.IO;
using System.Threading.Tasks;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Maple2.Server.Web.Controllers;

[Route("")]
public class WebController : ControllerBase {
    private static readonly string SolutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    private static readonly string RootDir = Path.Combine(SolutionDir, "Maple2.Server.Web/Data");

    private readonly WebStorage webStorage;

    public WebController(WebStorage webStorage) {
        this.webStorage = webStorage;
    }

    [HttpPost("urq.aspx")]
    public async Task<IResult> Upload() {
        Stream bodyStream = Request.Body;
        var memoryStream = new MemoryStream();
        await bodyStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0; // reset position to beginning of stream before returning
        if (memoryStream.Length == 0) {
            return Results.BadRequest("Request was empty");
        }

        IByteReader packet = new ByteReader(memoryStream.ToArray());
        packet.ReadInt();
        var type = (UgcType) packet.ReadInt();
        packet.ReadLong();
        long characterId = packet.ReadLong();
        long ugcUid = packet.ReadLong();
        int id = packet.ReadInt(); // item id, guild id, others?
        packet.ReadInt();
        packet.ReadLong();

        byte[] fileBytes = packet.ReadBytes(packet.Available);

        if (ugcUid != 0) {
            using WebStorage.Request db = webStorage.Context();
            UgcResource? resource = db.GetUgc(ugcUid);
            if (resource == null) {
                return Results.NotFound($"{ugcUid} does not exist.");
            }
            if (System.IO.File.Exists(resource.Path)) {
                return Results.Conflict("resource already exists.");
            }

            await System.IO.File.WriteAllBytesAsync(resource.Path, fileBytes);
            return Results.Text($"0,{resource.Path}");
        }

        return type switch {
            UgcType.ProfileAvatar => UploadProfileAvatar(fileBytes, characterId),
            _ => HandleUnknownMode(type),
        };
    }

    private static IResult UploadProfileAvatar(byte[] fileBytes, long characterId) {
        string filePath = $"{RootDir}/profiles/{characterId}/";
        try {
            // Deleting old files in the character folder
            if (Path.Exists(filePath)) {
                Directory.Delete(filePath, true);
            }
            Directory.CreateDirectory(filePath);
        } catch (Exception ex) {
            Log.Error(ex, "Failed preparing directory: {Path}", filePath);
            return Results.Problem("Internal Server Error", statusCode: 500);
        }

        string uniqueFileName = Guid.NewGuid().ToString();
        System.IO.File.WriteAllBytes($"{filePath}/{uniqueFileName}.png", fileBytes);
        return Results.Text($"0,data/profiles/avatar/{characterId}/{uniqueFileName}.png");
    }

    private static IResult HandleUnknownMode(UgcType mode) {
        Log.Logger.Warning("Invalid upload mode: {Mode}", mode);
        return Results.BadRequest($"Invalid upload mode: {mode}");
    }
}
