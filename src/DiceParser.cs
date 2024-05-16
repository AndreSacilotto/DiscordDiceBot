using System.Text;
using System.Text.RegularExpressions;

namespace DiscordDiceRoller;

public static partial class DiceParser
{
    public const string ROLL_HELP = @"```md
## Other:
    dr = roll a random positive number
    dh = send this message

### Items:
    +{} = positive
    -{} = negative
    {}d{} = dice modifier
    &{} = against or shift from [don't support shift]

### Roll Modifiers:
    !{} = minimal dice roll for explode (default 20)
    ?{} = maximun times a explosion can occur (default 1)
    l{} = drop lowest X (default 0)
    h{} = drop highest X (default 0)
```
";

    #region Regex
    private static char[] validSymbols = new char[]
    {
        'd',
        '0','1','2','3','4','5','6','7','8','9',
        '+','-','!','?','t','&','l','h'
    };

    // 1st select dices | 2st mod with negative | 3st mod no negative
    [GeneratedRegex(@"-?\d*d\d+|[&]-?\d+(?!\d*d\d+)|[+\-!?lh]\d+(?!\d*d\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ModifiersRegex();

    [GeneratedRegex(@"^-?\d*d\d+", RegexOptions.CultureInvariant)]
    public static partial Regex DiceCommandRegex();

    #endregion Regex

    private static RollDice defaultRollDice = new(new(Dice.D20), 20, 1, 0, 0);

    public static StringBuilder RemoveInvalidChars(ReadOnlySpan<char> input)
    {
        StringBuilder sb = new(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (validSymbols.Contains(ch))
                sb.Append(ch);
        }
        return sb;
    }

    public static string SingleDiceParse(int sides) => $"` {new Dice(sides).Roll()} `";

    private static bool IsDiceSyntax(char ch) => ch == 'd' || char.IsAsciiDigit(ch);
    public static string RollParse(ReadOnlySpan<char> command, TextBuilds buildLike)
    {
        // parse commands
        command = command.ToString().ToLowerInvariant().AsSpan();
        var cmd = RemoveInvalidChars(command).ToStringSpan();

        // do things with commands
        var diceRolls = new List<RollResults>(1);
        var modifiers = new RollModifier();

        // TODO: suport negative shifts
        var shifts = new List<int>();

        RollDice? roll = null;
        foreach (var c in ModifiersRegex().EnumerateMatches(cmd))
        {
            var match = cmd.Slice(c.Index, c.Length);
            var first = match[0];

            if (IsDiceSyntax(first) || (first == '-' && match.Length > 2 && IsDiceSyntax(match[1])))
            {
                // dY, XdY
                int start = 0;
                bool isNegative = first == '-'; // -dY, -XdY
                if (isNegative)
                {
                    start = 1;
                    first = match[1];
                }

                int sides, amount = 1;

                if (first == 'd')
                {
                    sides = int.Parse(match.Slice(start + 1));
                }
                else if (char.IsAsciiDigit(first))
                {
                    var split = match.Slice(start).ToString().Split('d');
                    amount = int.Parse(split[0]);
                    sides = int.Parse(split[1]);
                }
                else
                    return "error: invalid dice: " + match.ToString();

                sides = Math.Clamp(sides, 1, 100);
                amount = Math.Clamp(amount, 1, 100);

                roll = defaultRollDice.DeepCopy();
                roll.diceGroup = new(new(sides, 1, isNegative), amount);

                diceRolls.Add(new() { Source = roll });
            }
            else if (int.TryParse(match.Slice(1), out var result)) //X{}
            {
                switch (first)
                {
                    case '+':
                    modifiers.Add(result);
                    continue;
                    case '-':
                    modifiers.Add(-result);
                    continue;
                    case '&':
                    shifts.Add(result);
                    continue;
                }

                if (roll is null)
                    return "error: no dice for first: " + match.ToString();

                if (first == '!')
                {
                    roll.explodeOn = Math.Clamp(Math.Abs(result), 0, 100);
                }
                else if (first == '?')
                {
                    roll.explosionTimes = Math.Clamp(Math.Abs(result), 0, 99);
                }
                else if (first == 'l')
                {
                    roll.dropLow = Math.Clamp(Math.Abs(result), 0, 99);
                }
                else if (first == 'h')
                {
                    roll.dropHigh = Math.Clamp(Math.Abs(result), 0, 99);
                }
                else
                    return "error: invalid first: " + match.ToString();
            }
            else
                return "error: invalid command: " + match.ToString();
        }

        // calculate roll items
        var rollsTotal = 0;
        foreach (var dr in diceRolls)
        {
            dr.Rolls = dr.Source.CalculateRolls(out var sum).ToArray();
            dr.RollSum = sum;
            rollsTotal += dr.RollSum;
        }
        var final = rollsTotal + modifiers.Sum;

        //return new ParsedRoll(final, rollsTotal, diceRolls, modifiers, shifts);

        return buildLike switch
        {
            TextBuilds.Rollem => BuildRollem(final, rollsTotal, diceRolls, modifiers.Sum, modifiers.Modifiers, shifts).ToString(),
            TextBuilds.Lines => BuildLines(final, rollsTotal, diceRolls, modifiers.Sum, modifiers.Modifiers, shifts).ToString(),
            TextBuilds.LinesAnsi => BuildLinesAnsi(final, rollsTotal, diceRolls, modifiers.Sum, modifiers.Modifiers, shifts).ToString(),
            TextBuilds.Basic => BuildBasic(final, rollsTotal, diceRolls, modifiers.Sum, modifiers.Modifiers, shifts).ToString(),
            TextBuilds.Table => BuildTable(final, rollsTotal, diceRolls, modifiers.Sum, modifiers.Modifiers, shifts).ToString(),
            _ => "INVALID BUILD",
        }; ;
    }

    #region Building Textual Response

    public class RollResults
    {
        public required RollDice Source { get; init; }
        public RollDice.SingleRoll[] Rolls { get; set; } = Array.Empty<RollDice.SingleRoll>();
        public int RollSum { get; set; } = 0;
    }
    //public record ParsedRoll(int Final, int RollsTotal, List<RollResults> DiceRolls, int ModiferSum, List<int> Modifiers, List<int> Shifts);

    public enum TextBuilds
    {
        Basic,
        Rollem,
        Lines,
        LinesAnsi,
        Table,
    }

    private static StringBuilder BuildBasic(int final, int rollsTotal, List<RollResults> diceRolls, int modiferTotal, List<int> modifiers, List<int> shifts)
    {
        StringBuilder sb = new(4);
        sb.AddHeading(1).Append(final);
        if (shifts.Count > 0)
        {
            sb.Append(" :");
            foreach (var shift in shifts)
                sb.Append($" `{final - shift}`");
        }
        return sb;
    }

    private static StringBuilder BuildRollem(int final, int rollsTotal, List<RollResults> diceRolls, int modiferTotal, List<int> modifiers, List<int> shifts)
    {
        const string listSeparator = ", ";
        StringBuilder sb = new(50);

        sb.Append($"` {final,-2} ` ⟵");

        if (diceRolls.Count > 0)
        {
            sb.AppendSpace();

            const string rollSeparator = " + ";
            var wrap = new AppendWrapper();
            foreach (var dr in diceRolls)
            {
                sb.OpenMask();
                foreach (var r in dr.Rolls)
                {
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Explosion))
                        wrap.Add(DiscordText.UNDERLINE);
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Dropped))
                        wrap.Add(DiscordText.STRIKETHROUGH);
                    if (UtilBit.HasAnyFlag((int)r.Mask, (int)(RollDice.RollMask.Minimal | RollDice.RollMask.Maximum)))
                        wrap.Add(DiscordText.BOLD);
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Exploded))
                        wrap.Add(DiscordText.ITALIC);

                    wrap.Wrap(sb, r.Roll.ToString());
                    wrap.Clear();

                    sb.Append(listSeparator);
                }
                sb.Length -= listSeparator.Length;
                sb.CloseMask();

                sb.Append($" {dr.Source.diceGroup}");
                sb.Append(rollSeparator);
            }
            sb.Length -= rollSeparator.Length;
        }

        if (modifiers.Count > 0)
        {
            sb.AppendSpace();
            foreach (var m in modifiers)
                sb.Append(m.ToString(RollModifier.INT_PLUS)).AppendSpace();
            sb.Length--;
        }

        if (shifts.Count > 0)
        {
            sb.Append(" Shift:");
            foreach (var shift in shifts)
                sb.Append($" {final - shift,-2}").Append(listSeparator);
            sb.Length -= listSeparator.Length;
        }

        return sb;
    }

    private static StringBuilder BuildLines(int final, int rollsTotal, List<RollResults> diceRolls, int modiferTotal, List<int> modifiers, List<int> shifts)
    {
        StringBuilder sb = new(50);

        var sep = ", ".AsSpan();

        sb.Append($"` {final,-2} `");

        if (shifts.Count > 0)
        {
            sb.AppendTab().Append("Shift:");
            foreach (var shift in shifts)
                sb.AppendSpace().Append($"` {final - shift,-2} ` [{shift}]");
        }

        if (diceRolls.Count > 0)
        {
            sb.AppendLine().Append($"` {rollsTotal,-2} `").AppendTab();
            var wrap = new AppendWrapper();
            foreach (var dr in diceRolls)
            {
                sb.Append($"` {dr.RollSum,-2} ` ({dr.Source.diceGroup}) ");
                sb.OpenMask();
                foreach (var r in dr.Rolls)
                {
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Explosion))
                        wrap.Add(DiscordText.UNDERLINE);
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Dropped))
                        wrap.Add(DiscordText.STRIKETHROUGH);
                    if (UtilBit.HasAnyFlag((int)r.Mask, (int)(RollDice.RollMask.Minimal | RollDice.RollMask.Maximum)))
                        wrap.Add(DiscordText.BOLD);
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Exploded))
                        wrap.Add(DiscordText.ITALIC);

                    wrap.Wrap(sb, r.Roll.ToString());
                    wrap.Clear();

                    sb.Append(sep);
                }
                sb.Length -= sep.Length;
                sb.CloseMask();
            }
        }

        if (modifiers.Count > 0)
        {
            sb.AppendLine().Append($"` {modiferTotal,-2} `").AppendTab();
            sb.OpenMask();
            foreach (var m in modifiers)
                sb.Append(m.ToString(RollModifier.INT_PLUS)).Append(sep);
            sb.Length -= sep.Length;
            sb.CloseMask();
        }

        return sb;
    }

    private static StringBuilder BuildLinesAnsi(int final, int rollsTotal, List<RollResults> diceRolls, int modiferTotal, List<int> modifiers, List<int> shifts)
    {
        const string separator = ", ";
        const string divisor = " | ";
        static StringBuilder AppendAnsiSign(StringBuilder sb, int num)
        {
            var numStr = num.ToString(RollModifier.INT_PLUS);
            if (num < 0)
                sb.OpenAnsi(color: DiscordText.AnsiColor.Red);
            else if (num > 0)
                sb.OpenAnsi(color: DiscordText.AnsiColor.Green);
            else
                return sb.Append(numStr);
            return sb.Append(numStr).CloseAnsi();
        }

        StringBuilder sb = new(50);

        sb.OpenCodeBlockAnsi();

        sb.OpenAnsi(color: DiscordText.AnsiColor.Yellow).Append($"{final}").CloseAnsi();

        if (diceRolls.Count > 0)
        {
            sb.AppendLine().Append(rollsTotal).AppendTab();
            foreach (var dr in diceRolls)
            {
                sb.Append($"{dr.RollSum} <= {dr.Source.diceGroup} ");
                sb.OpenMask();
                foreach (var r in dr.Rolls)
                {
                    if (r.Mask.HasFlag(RollDice.RollMask.None))
                    {
                        sb.Append(r.Roll);
                    }
                    else
                    {
                        if (r.Mask.HasFlag(RollDice.RollMask.Explosion))
                            sb.OpenAnsi(DiscordText.AnsiFormat.Underline);
                        if (r.Mask.HasFlag(RollDice.RollMask.Minimal))
                            sb.OpenAnsi(DiscordText.AnsiFormat.Bold, DiscordText.AnsiColor.Blue);
                        if (r.Mask.HasFlag(RollDice.RollMask.Maximum))
                            sb.OpenAnsi(DiscordText.AnsiFormat.Bold, DiscordText.AnsiColor.Yellow);
                        if (r.Mask.HasFlag(RollDice.RollMask.Dropped))
                            sb.OpenAnsi(color: DiscordText.AnsiColor.Red);
                        if (r.Mask.HasFlag(RollDice.RollMask.Exploded))
                            sb.OpenAnsi(color: DiscordText.AnsiColor.BgGray);

                        sb.Append(r.Roll);
                        sb.CloseAnsi();
                    }
                    sb.Append(separator);
                }
                sb.Length -= separator.Length;
                sb.CloseMask();
                sb.Append(divisor);
            }
            sb.Length -= divisor.Length;
        }

        if (modifiers.Count > 0)
        {
            sb.AppendLine();
            AppendAnsiSign(sb, modiferTotal).AppendTab().OpenMask();
            foreach (var m in modifiers)
                AppendAnsiSign(sb, m).Append(separator);
            sb.Length -= separator.Length;
            sb.CloseMask();
        }

        if (shifts.Count > 0)
        {
            sb.AppendLine().Append("Shifts: ");
            foreach (var shift in shifts)
            {
                sb.OpenAnsi(color: DiscordText.AnsiColor.Cyan)
                    .Append($"{(final - shift).ToString(RollModifier.INT_PLUS)} from {shift}")
                    .CloseAnsi()
                    .Append(divisor);
            }
            sb.Length -= divisor.Length;
        }

        sb.CloseCodeBlock();
        return sb;
    }
    
    private static StringBuilder BuildTable(int final, int rollsTotal, List<RollResults> diceRolls, int modiferTotal, List<int> modifiers, List<int> shifts)
    {
        // result | shift | diceSum | dice | modifierSum | modifiers

        const string divisor = " | ";
        StringBuilder sb = new();

        const int columns = 6;
        List<string?> headers = new(columns) { "Result" };
        List<string?> data = new(columns) { final.ToString() };

        if (shifts.Count > 0)
        {
            headers.Add("Shift");
            foreach (var shift in shifts)
                data.Add($"{final - shift} ({shift})");
        }

        if (diceRolls.Count > 0)
        {
            headers.Add("Sum <-");
            data.Add(rollsTotal.ToString());
            headers.Add("Dice");
            foreach (var dr in diceRolls)
                sb.Append($"{dr.RollSum} ({dr.Source.diceGroup}) [{string.Join(", ", dr.Rolls.Select(x => x.Roll))}] ");
            sb.Length--;
            data.Add(sb.Submit());
        }

        if(modifiers.Count > 0)
        {
            headers.Add("Sum <-");
            data.Add(modiferTotal.ToString());
            headers.Add("Modifiers");
            data.Add(string.Join(", ", modifiers.Select(x => x.ToString(RollModifier.INT_PLUS))));
        }

        sb.OpenCodeBlockAnsi();
        
        var len = Math.Min(headers.Count, data.Count);
        var last = columns - 1;

        var colsWidth = new int[len];
        for (int i = 0; i < len; i++)
            colsWidth[i] = Math.Max(headers[i]!.Length, data[i]!.Length);

        sb.OpenAnsi(DiscordText.AnsiFormat.Bold, DiscordText.AnsiColor.White);
        for (int i = 0; i < last; i++)
            sb.Append(headers[i]!.PadCenter(colsWidth[i])).Append(divisor);
        sb.Append(headers[last]!.PadCenter(colsWidth[last]));
        sb.CloseAnsi();

        sb.AppendLine();

        for (int i = 0; i < last; i++)
            sb.Append(data[i]!.PadCenter(colsWidth[i])).Append(divisor);
        sb.Append(data[last]!.PadCenter(colsWidth[last]));

        sb.CloseCodeBlock();

        return sb;
    }

    #endregion

}
