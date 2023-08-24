using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Web.Constants;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Maple2.Server.Web.Endpoints;

public static class UploadEndpoint {
    public static async Task<IResult> Post(HttpRequest request) {
        Stream bodyStream = request.Body;

        MemoryStream memoryStream = await CopyStream(bodyStream);
        if (memoryStream.Length == 0) {
            return Results.BadRequest();
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

        byte[]? fileBytes = packet.ReadBytes(packet.Available);

        return type switch {
            UgcType.ProfileAvatar => HandleProfileAvatar(fileBytes, characterId),
            _ => HandleUnknownMode(type)
        };
    }

    private static IResult HandleProfileAvatar(byte[] fileBytes, long characterId) {
        string filePath = $"{Target.DataDir}/profiles/{characterId}/";
        Directory.CreateDirectory(filePath);

        string uniqueFileName = Guid.NewGuid().ToString();

        // Deleting old files in the character folder
        var directory = new DirectoryInfo(filePath);
        foreach (FileInfo file in directory.GetFiles()) {
            file.Delete();
        }

        File.WriteAllBytes($"{filePath}/{uniqueFileName}.png", fileBytes);
        return Results.Text($"0,data/profiles/avatar/{characterId}/{uniqueFileName}.png");
    }

    private static IResult HandleUnknownMode(UgcType mode) {
        Log.Logger.Warning("Unknown upload mode: {mode}", mode);
        return Results.BadRequest();
    }

    private static async Task<MemoryStream> CopyStream(Stream input) {
        var output = new MemoryStream();
        await input.CopyToAsync(output);
        output.Position = 0; // reset position to beginning of stream before returning
        return output;
    }
}
