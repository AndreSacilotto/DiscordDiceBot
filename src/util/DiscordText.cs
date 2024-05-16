using System.Text;

namespace DiscordDiceRoller;

internal static class DiscordText
{
    #region Consts

    public const char EMOJI = ':';

    public const char HEADING = '#';
    public const char CODE = '`';
    public const char BLOCKQUOTES = '>';
    public const string CODE_BLOCK = "```";
    public const char ORDERED_LIST = '.'; // 1.
    public const char UNORDERED_LIST = '+';

    public const char ITALIC = '*';
    public const string BOLD = "**";
    public const string ITALIC_BOLD = "***";
    public const string SPOILERS = "||";
    public const string UNDERLINE = "__";
    public const string STRIKETHROUGH = "~~";

    #endregion

    #region Extra

    public static StringBuilder AppendItalic(this StringBuilder builder) => builder.Append(ITALIC);

    public static StringBuilder AppendBold(this StringBuilder builder) => builder.Append(BOLD);

    public static StringBuilder AppendItalicBold(this StringBuilder builder) => builder.Append(ITALIC_BOLD);

    public static StringBuilder AppendSpoilers(this StringBuilder builder) => builder.Append(SPOILERS);

    public static StringBuilder AppendUnderline(this StringBuilder builder) => builder.Append(UNDERLINE);

    public static StringBuilder AppendStrikethrough(this StringBuilder builder) => builder.Append(STRIKETHROUGH);

    public static StringBuilder AppendCode(this StringBuilder builder) => builder.Append(CODE);

    #endregion

    #region Extra: Open/Close

    public static StringBuilder OpenMask(this StringBuilder builder) => builder.Append('[');

    public static StringBuilder CloseMask(this StringBuilder builder) => builder.Append(']');

    public static StringBuilder OpenMaskedLink(this StringBuilder builder) => builder.Append('(');

    public static StringBuilder CloseMaskedLink(this StringBuilder builder) => builder.Append(')');

    public static StringBuilder OpenCloseUnembedLink(this StringBuilder builder, ReadOnlySpan<char> link)
    {
        builder.Append('<');
        builder.Append(link);
        builder.Append('>');
        return builder;
    }

    #endregion

    #region Extra: Add
    public static StringBuilder AddHeading(this StringBuilder builder, int number = 1)
    {
        var n = Math.Clamp(number, 1, 6);
        builder.Append(HEADING, n);
        builder.AppendSpace();
        return builder;
    }
    public static StringBuilder AddBlockquote(this StringBuilder builder)
    {
        builder.Append(BLOCKQUOTES);
        builder.AppendSpace();
        return builder;
    }

    public static StringBuilder AddOrderedList(this StringBuilder builder, byte number = 1)
    {
        builder.Append(number);
        builder.Append(ORDERED_LIST);
        builder.AppendSpace();
        return builder;
    }

    public static StringBuilder AddUnorderedList(this StringBuilder builder)
    {
        builder.Append(UNORDERED_LIST);
        builder.AppendSpace();
        return builder;
    }

    #endregion

    #region CodeBlock

    // https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md
    public static StringBuilder OpenCodeBlock(this StringBuilder builder, string highlight = "") =>
        builder.Append(CODE_BLOCK).Append(highlight).AppendLine();

    public static StringBuilder CloseCodeBlock(this StringBuilder builder) =>
        builder.AppendLine().Append(CODE_BLOCK).AppendLine();

    // https://gist.github.com/kkrypt0nn/a02506f3712ff2d1c8ca7c9e0aed7c06
    public enum AnsiFormat
    {
        Normal = 0,
        Bold = 1,
        Underline = 4,
    }

    public enum AnsiColor
    {
        Default = 0,

        // Text
        Gray = 30,
        Red = 31,
        Green = 32,
        Yellow = 33,
        Blue = 34,
        Pink = 35,
        Cyan = 36,
        White = 37,

        // Background
        BgFireflyDarkBlue = 40,
        BgOrange = 41,
        BgMarbleBlue = 42,
        BgGreyishTurquoise = 43,
        BgGray = 44,
        BgIndigo = 45,
        BgLightGray = 46,
        BgWhite = 47,
    }

    public static StringBuilder OpenCodeBlockAnsi(this StringBuilder builder) => OpenCodeBlock(builder, "ansi");
    public static StringBuilder OpenAnsi(this StringBuilder builder, AnsiFormat format = AnsiFormat.Normal, AnsiColor color = AnsiColor.Default) => builder.Append($"\u001b[{(int)format};{(int)color}m");
    // you need to only close once
    public static StringBuilder CloseAnsi(this StringBuilder builder) => builder.Append("\u001b[0m");

    #endregion
}
