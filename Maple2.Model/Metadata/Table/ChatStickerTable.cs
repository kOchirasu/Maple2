﻿using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record ChatStickerTable(IReadOnlyDictionary<int, ChatStickerMetadata> Entries) : Table(Discriminator.ChatStickerTable);

public record ChatStickerMetadata(int Id, int GroupId);
