using System.Text;

namespace DiscordDiceRoller;

internal static class StringBuilderExt
{
    public const char TAB = '\t';
    public const char SPACE = ' ';

    public static StringBuilder AppendSpace(this StringBuilder builder) => builder.Append(SPACE);
    public static StringBuilder AppendTab(this StringBuilder builder) => builder.Append(TAB);

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

    public static StringBuilder ConstructSingleRowTable(IList<string> headers, IList<string> data)
    {
        // extra data columns will be ignored
        const string divisor = " | ";
        StringBuilder sb = new();

        var len = Math.Min(headers.Count, data.Count);
        var last = len - 1;

        if (len == 0)
            throw new Exception("Empty Header or Row");

        var colsWidth = new int[len];
        for (int i = 0; i < len; i++)
            colsWidth[i] = Math.Max(headers[i].Length, data[i].Length);

        for (int i = 0; i < last; i++)
            sb.Append(headers[i].PadLeft(colsWidth[i])).Append(divisor);
        sb.Append(headers[last].PadLeft(colsWidth[last]));

        sb.AppendLine();

        for (int i = 0; i < last; i++)
            sb.Append(data[i].PadLeft(colsWidth[i])).Append(divisor);
        sb.Append(data[last].PadLeft(colsWidth[last]));

        return sb;
    }
}
