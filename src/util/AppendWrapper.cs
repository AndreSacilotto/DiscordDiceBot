using System.Text;

namespace DiscordDiceRoller;

public class AppendWrapper
{
    public const char NULL_CHAR = '\0';

    // Opt - 1
    //public readonly StringBuilder prefix = new();
    //public readonly StringBuilder postfix = new();

    // Opt - 2
    //public interface IWrap
    //{
    //    string Prefix { get; }
    //    string Postfix { get; }
    //}
    //record class W1(string Wrapper) : IWrap
    //{
    //    public string Postfix => Wrapper;
    //    public string Prefix => Wrapper;
    //}
    //record class W2(string Prefix, string Postfix) : IWrap;

    // Opt - 3
    public readonly record struct Wrapper(char Prefix, char Postfix = NULL_CHAR)
    {
        public Wrapper(char wrap) : this(wrap, wrap) { }
    }

    public List<Wrapper> Wraps { get; init; } = new();

    public void Clear() => Wraps.Clear();

    //
    public Wrapper Add(char wrap)
    {
        Wrapper w = new(wrap);
        Wraps.Add(w);
        return w;
    }
    public void Add(ReadOnlySpan<char> both)
    {
        foreach (var w in both)
            Add(w);
    }

    public Wrapper Add(char prefix, char postfix)
    {
        Wrapper w = new(prefix, postfix);
        Wraps.Add(w);
        return w;
    }
    //

    public void AddPrefix(ReadOnlySpan<char> prefix)
    {
        foreach (var item in prefix)
            Add(item, NULL_CHAR);
    }

    public void AddPostfix(ReadOnlySpan<char> posfix)
    {
        foreach (var item in posfix)
            Add(NULL_CHAR, item);
    }

    //
    public void AddRange(ReadOnlySpan<char> prefix, ReadOnlySpan<char> postfix)
    {
        for (int i = 0; i < prefix.Length; i++)
            Add(prefix[i], NULL_CHAR);

        for (int i = 0; i < postfix.Length; i++)
            Add(NULL_CHAR, postfix[i]);
    }

    public void AddRange(IEnumerable<char> prefix, IEnumerable<char> postfix)
    {
        foreach (var item in prefix)
            Add(item, NULL_CHAR);

        foreach (var item in postfix)
            Add(NULL_CHAR, item);
    }

    public AppendWrapper Simplified()
    {
        var pre = new Queue<char>();
        var pos = new Queue<char>();
        var newList = new List<Wrapper>(Wraps.Count);

        for (int i = 0; i < Wraps.Count; i++)
        {
            var p = Wraps[i];
            bool pr = p.Prefix != NULL_CHAR;
            bool po = p.Postfix != NULL_CHAR;

            if (pr)
            {
                if (po)
                    newList.Add(p);
                else
                {
                    if (pos.Count > 0)
                        newList.Add(new(p.Prefix, pos.Dequeue()));
                    else
                        pre.Enqueue(p.Prefix);
                }
            }
            else if (po)
            {
                if (pre.Count > 0)
                    newList.Add(new(p.Postfix, pos.Dequeue()));
                else
                    pos.Enqueue(p.Postfix);
            }
        }

        foreach (var item in pre)
            newList.Add(new(item, NULL_CHAR));

        foreach (var item in pos)
            newList.Add(new(NULL_CHAR, item));

        return new AppendWrapper() { Wraps = newList };
    }


    public StringBuilder Wrap(StringBuilder builder, string str)
    {
        builder.EnsureCapacity(builder.Capacity + Wraps.Count * 2);
        for (int i = 0; i < Wraps.Count; i++)
            builder.Append(Wraps[i].Prefix);
        builder.Append(str);
        for (int i = Wraps.Count - 1; i >= 0; i--)
            builder.Append(Wraps[i].Postfix);
        return builder;
    }

    public StringBuilder ToString(string str)
    {
        var sb = new StringBuilder();
        return Wrap(sb, str);
    }
}
