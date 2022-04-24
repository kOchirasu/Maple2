using System;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game; 

public class Player : IByteSerializable {
    public readonly Account Account;
    public readonly Character Character;

    
    
    public Player(Account account, Character character) {
        Account = account;
        Character = character;
    }

    public void WriteTo(IByteWriter writer) {
        
    }

    public void ReadFrom(IByteReader reader) {
        throw new System.NotImplementedException();
    }
}
