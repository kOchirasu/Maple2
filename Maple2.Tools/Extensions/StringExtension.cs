﻿using Pastel;

namespace Maple2.Tools.Extensions;

public static class StringExtension {
    public static string ColorGreen(this string input) {
        return input.Pastel("#aced66");
    }

    public static string ColorRed(this string input) {
        return input.Pastel("#E05561");
    }

    public static string ColorYellow(this string input) {
        return input.Pastel("#FFE212");
    }
}
