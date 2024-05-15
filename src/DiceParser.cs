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

    [GeneratedRegex(@"-?\d*d\d+|[+\-!?t&lh]\d+(?!\d*d\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ModifiersRegex();

    [GeneratedRegex(@"^-?\d*d\d+", RegexOptions.CultureInvariant)]
    public static partial Regex DiceCommandRegex();

    #endregion Regex

    private static RollDice defaultRollDice = new(new(Dice.D20), 20, 1, 0, 0);

    public static string SingleDiceParse(int sides) => $"` {new Dice(sides).Roll()} `";

    public class RollResults {
        public required RollDice RollDice { get; init; }
        public int RollSum { get; set; } = 0;
        public RollDice.SingleRoll[] Rolls { get; set; } = Array.Empty<RollDice.SingleRoll>();
    }

    public static string RollParse(ReadOnlySpan<char> command)
    {
        // parse commands
        var commandStr = command.ToString().ToLowerInvariant();
        var sb = new StringBuilder(commandStr.Length);
        foreach (var c in commandStr)
            if (validSymbols.Contains(c))
                sb.Append(c);

        var cmd = sb.Submit().AsSpan();

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

            if (first == 'd' || char.IsAsciiDigit(first))
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

        // create the response
        var sep = ", ".AsSpan();

        sb.Append($"` {final,-2} `");

        if (shifts.Count > 0)
        {
            sb.AppendTab().Append("Shift:");
            foreach (var shift in shifts)
                sb.AppendSpace().AppendBold().Append(final - shift).AppendBold().Append($" [{shift}]");
        }

        if (diceRolls.Count > 0)
        {
            sb.AppendLine().Append($"` {rollsSum,-2} `").AppendTab();
            foreach (var dr in diceRolls)
            {
                sb.Append($"` {dr.RollSum,-2} `").AppendSpace();
                sb.OpenMask();
                foreach (var r in dr.Rolls)
                {
                    var under = r.Mask.HasFlag(RollDice.RollMask.Explosion);
                    var italic = r.Mask.HasFlag(RollDice.RollMask.Exploded);
                    var strike = r.Mask.HasFlag(RollDice.RollMask.Dropped);
                    var bold = (r.Mask & (RollDice.RollMask.Minimal | RollDice.RollMask.Maximun)) != 0;

                    if (under) sb.AppendUnderline();
                    if (strike) sb.AppendStrikethrough();
                    if (bold) sb.AppendBold();
                    if (italic) sb.AppendItalic();

                    sb.Append(r.Roll);

                    if (italic) sb.AppendItalic();
                    if (bold) sb.AppendBold();
                    if (strike) sb.AppendStrikethrough();
                    if (under) sb.AppendUnderline();

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

        return sb.ToString();
    }


}