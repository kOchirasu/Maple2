using System.Diagnostics;
using Maple2.Database.Context;
using Maple2.Database.Extensions;
using Maple2.Database.Model.Metadata;
using Maple2.File.Ingest.Mapper;
using Maple2.File.IO;
using Maple2.File.Parser.Tools;
using Microsoft.EntityFrameworkCore;

const string locale = "NA";
const string env = "Live";
const string xmlPath = @"C:\Nexon\Library\Library\maplestory2\appdata\Data\Xml.m2d";
const string exportedPath = @"C:\Nexon\Library\Library\maplestory2\appdata\Data\Resource\Exported.m2d";

Console.OutputEncoding = System.Text.Encoding.UTF8;

string? dataDbConnection = Environment.GetEnvironmentVariable("DATA_DB_CONNECTION");
if (dataDbConnection == null) {
    throw new ArgumentException("DATA_DB_CONNECTION environment variable was not set");
}

using var xmlReader = new M2dReader(xmlPath);
using var exportedReader = new M2dReader(exportedPath);

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(dataDbConnection, ServerVersion.AutoDetect(dataDbConnection)).Options;

using var metadataContext = new MetadataContext(options);
metadataContext.Database.EnsureCreated();

// Filter Xml results based on feature settings.
Filter.Load(xmlReader, locale, env);

// new NpcScriptGenerator(xmlReader).Generate();
// new NpcScriptGenerator(xmlReader).GenerateEvent();

UpdateDatabase(metadataContext, new AnimationMapper(xmlReader));
UpdateDatabase(metadataContext, new ItemMapper(xmlReader));
UpdateDatabase(metadataContext, new NpcMapper(xmlReader));
UpdateDatabase(metadataContext, new MapMapper(xmlReader));
UpdateDatabase(metadataContext, new MapEntityMapper(metadataContext, exportedReader));
UpdateDatabase(metadataContext, new QuestMapper(xmlReader));
UpdateDatabase(metadataContext, new RideMapper(xmlReader));
UpdateDatabase(metadataContext, new SkillMapper(xmlReader));
UpdateDatabase(metadataContext, new TableMapper(xmlReader));
UpdateDatabase(metadataContext, new UgcMapMapper(xmlReader));

// new MusicScoreParser(xmlReader).Parse().ToList();
// new MusicScoreParser(xmlReader).Parse().ToList();
// new AniKeyTextParser(xmlReader).Parse().ToList();
// new UgcMapParser(xmlReader).Parse().ToList();
// new UgcMapParser(xmlReader).ParseExported().ToList();
// new ScriptParser(xmlReader).ParseNpc().ToList();
// new ScriptParser(xmlReader).ParseQuest().ToList();
// new PetParser(xmlReader).Parse().ToList();
// new PetParser(xmlReader).ParseProperty().ToList();
//
// new AchieveParser(xmlReader).Parse().ToList();
// new AdditionalEffectParser(xmlReader).Parse().ToList();
// new ItemParser(xmlReader).Parse().ToList();
// new MapParser(xmlReader).Parse().ToList();
// new NpcParser(xmlReader).Parse().ToList();
// new QuestParser(xmlReader).Parse().ToList();
// new SkillParser(xmlReader).Parse().ToList();

void UpdateDatabase<T>(DbContext context, TypeMapper<T> mapper) where T : class {
    string? tableName = context.GetTableName<T>();
    Debug.Assert(!string.IsNullOrEmpty(tableName), $"Invalid table name: {tableName}");

    uint crc32C = mapper.Process();

    var checksum = context.Find<TableChecksum>(tableName);
    if (checksum != null) {
        if (checksum.Crc32C == crc32C) {
            Console.WriteLine($"Table '{tableName}' is up-to-date");
            return;
        }

        checksum.Crc32C = crc32C;
        Console.WriteLine($"Table '{tableName}' outdated");
        int result = context.Database.ExecuteSqlRaw(@$"DELETE FROM `{tableName}`");
        Console.WriteLine($"Removed Table '{tableName}' rows: {result}");
    }

    // Write entries to table
    foreach (T result in mapper.Results) {
        context.Add(result);
    }

    // Write checksum to table
    if (checksum == null) {
        context.Add(new TableChecksum {
            TableName = tableName,
            Crc32C = crc32C,
        });
    } else {
        context.Update(checksum);
    }

    context.SaveChanges();
}
