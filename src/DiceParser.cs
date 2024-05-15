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
    
    #region Classes
    public class RollResults
    {
        public required RollDice RollDice { get; init; }
        public int RollSum { get; set; } = 0;
        public RollDice.SingleRoll[] Rolls { get; set; } = Array.Empty<RollDice.SingleRoll>();
    }
    //public record ParsedRoll(int Final, int RollsSum, List<RollResults> DiceRolls, RollModifier Modifiers, List<int> Shifts);
    
    #endregion

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

                diceRolls.Add(new() { RollDice = roll });
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
        var rollsSum = 0;
        foreach (var dr in diceRolls)
        {
            dr.Rolls = dr.RollDice.CalculateRolls(out var sum).ToArray();
            dr.RollSum = sum;
            rollsSum += dr.RollSum;
        }
        var final = rollsSum + modifiers.Sum;

        //var pr = new ParsedRoll(final, rollsSum, diceRolls, modifiers, shifts);

        return buildLike switch
        {
            TextBuilds.Lines => BuildLinesRollem(final, rollsSum, diceRolls, modifiers, shifts).ToString(),
            TextBuilds.Ansi => BuildAnsi(final, rollsSum, diceRolls, modifiers, shifts).ToString(),
            TextBuilds.Rollem => BuildRollem(final, rollsSum, diceRolls, modifiers, shifts).ToString(),
            _ => "INVALID BUILD",
        };
    }

    #region Building Textual Response

    public enum TextBuilds
    {
        Rollem,
        Lines,
        Ansi,
        Table,
    }

    private static StringBuilder BuildAnsi(int final, int rollsSum, List<RollResults> diceRolls, RollModifier modifiers, List<int> shifts)
    {
        StringBuilder sb = new(50);
        sb.Append("[WIP]");
        return sb;
    }

    private static StringBuilder BuildRollem(int final, int rollsSum, List<RollResults> diceRolls, RollModifier modifiers, List<int> shifts)
    {
        StringBuilder sb = new(50);
        var sep = ", ".AsSpan();

        sb.Append($"` {final,-2} ` ⟵ ");

        if (diceRolls.Count > 0)
        {
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
                    if (UtilBit.HasAnyFlag((int)r.Mask, (int)(RollDice.RollMask.Minimal | RollDice.RollMask.Maximun)))
                        wrap.Add(DiscordText.BOLD);
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Exploded))
                        wrap.Add(DiscordText.ITALIC);

                    wrap.Wrap(sb, r.Roll.ToString());
                    wrap.Clear();

                    sb.Append(sep);
                }
                sb.Length -= sep.Length;
                sb.CloseMask();
                
                sb.Append($" {dr.RollDice.diceGroup}");
                sb.Append(" + ");
            }
            sb.Length -= 3;
        }

        if (modifiers.Modifiers.Count > 0)
        {
            sb.AppendSpace();
            foreach (var m in modifiers.AsString())
                sb.Append(m).AppendSpace();
            sb.Length--;
        }

        if (shifts.Count > 0)
        {
            sb.Append(" Shift:");
            foreach (var shift in shifts)
                sb.AppendSpace().Append($"{final - shift,-2}, ");
        }

        return sb;
    }

    private static StringBuilder BuildLinesRollem(int final, int rollsSum, List<RollResults> diceRolls, RollModifier modifiers, List<int> shifts)
    {
        StringBuilder sb = new(50);

        var sep = ", ".AsSpan();

        sb.Append($"` {final,-2} `");

        if (shifts.Count > 0)
        {
            sb.AppendTab().Append("Shift:");
            foreach (var shift in shifts)
                sb.AppendSpace().Append($"` {final - shift,-2} `").Append($" [{shift}]");
        }

        if (diceRolls.Count > 0)
        {
            sb.AppendLine().Append($"` {rollsSum,-2} `").AppendTab();
            var wrap = new AppendWrapper();
            foreach (var dr in diceRolls)
            {
                sb.Append($"` {dr.RollSum,-2} ` ({dr.RollDice.diceGroup}) ").AppendSpace();
                sb.OpenMask();
                foreach (var r in dr.Rolls)
                {
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Explosion))
                        wrap.Add(DiscordText.UNDERLINE);
                    if (UtilBit.HasFlag((int)r.Mask, (int)RollDice.RollMask.Dropped))
                        wrap.Add(DiscordText.STRIKETHROUGH);
                    if (UtilBit.HasAnyFlag((int)r.Mask, (int)(RollDice.RollMask.Minimal | RollDice.RollMask.Maximun)))
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

        if (modifiers.Modifiers.Count > 0)
        {
            sb.AppendLine().Append($"` {modifiers.Sum,-2} `").AppendTab();
            sb.OpenMask();
            foreach (var m in modifiers.AsString())
                sb.Append(m).Append(sep);
            sb.Length -= sep.Length;
            sb.CloseMask();
        }

        return sb;
    }

    #endregion


}