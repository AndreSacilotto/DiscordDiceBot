using System.Text;

namespace DiscordDiceRoller;

internal static class DiscordText
{
    #region Consts

    public const char TAB = '\t';
    public const char SPACE = ' ';

    public const char EMOJI = ':';

    public const char HEADING = '`';
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

    public static string Submit(this StringBuilder builder)
    {
        var str = builder.ToString();
        builder.Clear();
        return str;
    }
    public static ReadOnlySpan<char> SubmitSpan(this StringBuilder builder)
    {
        var span = builder.ToString().AsSpan();
        builder.Clear();
        return span;
    }
    public static ReadOnlySpan<char> ToStringSpan(this StringBuilder builder) => builder.ToString().AsSpan();

    #region Wrap
    public static StringBuilder AppendWrap(this StringBuilder builder, ReadOnlySpan<char> text, bool needReverse, params char[] items)
    {
        return AppendWrap(builder, text, needReverse, items.AsSpan());
    }
    public static StringBuilder AppendWrap(this StringBuilder builder, ReadOnlySpan<char> text, bool needReverse, params string[] items)
    {
        var charItems = string.Concat(items).ToCharArray();
        return AppendWrap(builder, text, needReverse, charItems);
    }
    public static StringBuilder AppendWrap(this StringBuilder builder, ReadOnlySpan<char> text, bool needReverse, Span<char> items)
    {
        builder.Append(items);
        builder.Append(text);
        if (needReverse)
            items.Reverse();
        builder.Append(items);
        return builder;
    }
    #endregion

    #region Extra
    public static StringBuilder AppendSpace(this StringBuilder builder) => builder.Append(SPACE);

    public static StringBuilder AppendTab(this StringBuilder builder) => builder.Append(TAB);

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
        AppendSpace(builder);
        return builder;
    }
    public static StringBuilder AddBlockquote(this StringBuilder builder)
    {
        builder.Append(BLOCKQUOTES);
        AppendSpace(builder);
        return builder;
    }

    public static StringBuilder AddOrderedList(this StringBuilder builder, byte number = 1)
    {
        builder.Append(number);
        builder.Append(ORDERED_LIST);
        AppendSpace(builder);
        return builder;
    }

    public static StringBuilder AddUnorderedList(this StringBuilder builder)
    {
        builder.Append(UNORDERED_LIST);
        AppendSpace(builder);
        return builder;
    }

    // https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md
    public static StringBuilder CodeBlock(this StringBuilder builder, ReadOnlySpan<char> code, string highlight = "")
    {
        builder.Append(CODE_BLOCK).AppendLine(highlight);
        builder.Append(code);
        builder.Append(CODE_BLOCK).AppendLine();
        return builder;
    }
    #endregion

    #region Ansi
    // https://gist.github.com/kkrypt0nn/a02506f3712ff2d1c8ca7c9e0aed7c06
    public static StringBuilder AnsiCodeBlockColor(this StringBuilder builder, ReadOnlySpan<char> code) => CodeBlock(builder, code, "ansi");

    public static StringBuilder OpenAnsi(this StringBuilder builder) => builder.Append("\u001b[0m");

    public static StringBuilder CloseAnsi(this StringBuilder builder, string color, string format) => builder.Append($"\u001b[{format};{color}m");
    #endregion


}
