using System;
using Maple2.Model.Common;
using Maple2.Model.Enum;

namespace Maple2.Model.User {
    public class Character {
        public DateTime LastModified { get; set; }
        
        public long Id { get; set; }
        public long AccountId { get; set; }
        public string Name { get; set; }

        public DateTime CreationTime { get; set; }
        public Gender Gender { get; set; }
        public Job Job { get; set; }
        public short Level { get; set; }
        public SkinColor SkinColor { get; set; }
        public long Experience { get; set; }
        public long RestExp { get; set; }
        public int MapId { get; set; }
        public int Title { get; set; }
        public short Insignia { get; set; }
    }
}
