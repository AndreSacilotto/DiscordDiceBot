using System.Runtime.CompilerServices;

namespace DiscordDiceRoller;

public static class Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsSpan(this in char ch) => new(ch);
}
