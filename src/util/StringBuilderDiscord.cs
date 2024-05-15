using System.Runtime.CompilerServices;
using System.Text;

namespace DiscordDiceRoller;

internal static class StringBuilderDiscord
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

    #region Extra
    public static string Submit(this StringBuilder builder)
    {
        var str = builder.ToString();
        builder.Clear();
        return str;
    }

    public static StringBuilder AppendSpace(this StringBuilder builder)
    {
        builder.Append(SPACE);
        return builder;
    }

    public static StringBuilder AppendTab(this StringBuilder builder)
    {
       builder.Append(TAB);
        return builder;
    }

    public static StringBuilder AppendItalic(this StringBuilder builder)
    {
        builder.Append(ITALIC);
        return builder;
    }

    public static StringBuilder AppendBold(this StringBuilder builder)
    {
        builder.Append(BOLD);
        return builder;
    }

    public static StringBuilder AppendItalicBold(this StringBuilder builder)
    {
        builder.Append(ITALIC_BOLD);
        return builder;
    }

    public static StringBuilder AppendSpoilers(this StringBuilder builder)
    {
        builder.Append(SPOILERS);
        return builder;
    }

    public static StringBuilder AppendUnderline(this StringBuilder builder)
    {
        builder.Append(UNDERLINE);
        return builder;
    }

    public static StringBuilder AppendStrikethrough(this StringBuilder builder)
    {
        builder.Append(STRIKETHROUGH);
        return builder;
    }

    public static StringBuilder AppendCode(this StringBuilder builder)
    {
        builder.Append(CODE);
        return builder;
    }

    #endregion

    #region Extra: Open/Close

    public static StringBuilder OpenMask(this StringBuilder builder)
    {
        builder.Append('[');
        return builder;
    }

    public static StringBuilder CloseMask(this StringBuilder builder)
    {
        builder.Append(']');
        return builder;
    }

    public static StringBuilder OpenMaskedLink(this StringBuilder builder)
    {
        builder.Append('(');
        return builder;
    }

    public static StringBuilder CloseMaskedLink(this StringBuilder builder)
    {
        builder.Append(')');
        return builder;
    }

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
        builder.Append(CODE_BLOCK);
        builder.Append(highlight);
        builder.AppendLine();
        builder.Append(code);
        builder.AppendLine();
        builder.Append(CODE_BLOCK);
        return builder;
    }
    #endregion

}
