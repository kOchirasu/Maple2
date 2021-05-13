using System;
using System.Collections.Generic;

namespace Maple2.Model.User {
    public class Account {
        public long Id { get; set; }
        public DateTime LastModified { get; set; }
        public long Merets { get; set; } 
        public int MaxCharacters { get; set; }
        public int PrestigeLevel { get; set; }
        public long PrestigeExp { get; set; }
        public long PremiumTime { get; set; }

        public ICollection<Character> Characters { get; set; }
    }
}
