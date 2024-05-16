using System.Runtime.CompilerServices;

namespace DiscordDiceRoller;

public static class Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsSpan(this in char ch) => new(ch);

    public static string PadCenter(this string text, int width)
    {
        if (text.Length >= width)
            return text;
        int padding = (width - text.Length) / 2;
        return text.PadLeft(text.Length + padding).PadRight(width);
    }
}
