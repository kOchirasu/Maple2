using System;

namespace Maple2.Model.Enum;

[Flags]
public enum TransferFlag {
    None = 0,
    Split = 2,
    Trade = 4,
    Bind = 8,
    LimitTrade = 16,
}
