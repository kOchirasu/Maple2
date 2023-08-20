﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Tools;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class AchievementMetadataStorage : MetadataStorage<int, AchievementMetadata> {
    private const int CACHE_SIZE = 2500; // ~2.2k total trophies

    public AchievementMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public bool TryGet(int id, [NotNullWhen(true)] out AchievementMetadata? achievement) {
        if (Cache.TryGet(id, out achievement)) {
            return true;
        }

        lock (Context) {
            // Double-checked locking
            if (Cache.TryGet(id, out achievement)) {
                return true;
            }

            achievement = Context.AchievementMetadata.Find(id);

            if (achievement == null) {
                return false;
            }

            Cache.AddReplace(id, achievement);
        }

        return true;
    }

    public IEnumerable<AchievementMetadata> GetMany(AchievementConditionType type) {
        return Context.AchievementMetadata
            .AsEnumerable()
            .Where(achievement => achievement.Grades.Values.Any(grade => grade.Condition.Type == type))
            .ToList();
    }
}