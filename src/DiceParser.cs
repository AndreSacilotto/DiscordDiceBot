using System.Text;
using System.Text.RegularExpressions;

namespace DiscordDiceRoller;

public static partial class DiceParser
{
    public const string ROLL_HELP = @"```md
## Other
    dr = roll a random positive number
    dh = send this message

### Items:
    +{} = positive
    -{} = negative
    {}d{} = dice modifier (default 1d20)

### Roll Modifiers:
    !{} = minimal dice roll for explode (default =sides)
    ?{} = maximun times a explosion can occur (default 1, max is 99)
    &{} = against or shift from (default 0)
    l{} = drop lowest X [WIP] (default 0)
    h{} = drop highest X [WIP] (default 0)
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
        var diceRolls = new List<RollDice>(1);
        var modifiers = new RollModifier();

        int? shift = null;
        RollDice? roll = null;
        foreach (var c in ModifiersRegex().EnumerateMatches(cmd))
        {
            var match = cmd.Slice(c.Index, c.Length);
            var first = match[0];

            if (first == 'd' || char.IsAsciiDigit(first))
            {
                roll = defaultRollDice.DeepCopy();

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

                Dice dice = new(sides, 1, isNegative);
                roll.diceGroup = new(dice, amount);
                diceRolls.Add(roll);
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
                    shift = result;
                    continue;
                }

                if (roll is null)
                    return "error: no dice for first: " + match.ToString();

                if (first == '!')
                {
                    roll.explodeOn = Math.Abs(result);
                }
                else if (first == '?')
                {
                    roll.explosionTimes = Math.Clamp(Math.Abs(result), 0, 99);
                }
                else if (first == 'l')
                {
                    roll.dropLow = Math.Abs(result);
                }
                else if (first == 'h')
                {
                    roll.dropHigh = Math.Abs(result);
                }
                else
                    return "error: invalid first: " + match.ToString();
            }
            else
                return "error: invalid command: " + match.ToString();
        }

        // create the response
        var sep = ", ".AsSpan();
        // TODO: sum rolls intead of making it separted
        foreach (var diceRoll in diceRolls)
        {
            var rolls = diceRoll.CalculateRolls();
            var rollSum = 0;
            foreach (var r in rolls)
                if (!r.Mask.HasFlag(RollDice.RollMask.Dropped))
                    rollSum += r.Roll;
            var final = rollSum + modifiers.Sum;

            sb.Append($"` {final} `").AppendLine();

            sb.Append($"` {rollSum} `").AppendSpace();
            sb.OpenMask();
            if (rolls.Count > 0)
            {
                foreach (var r in rolls)
                {
                    var under = r.Mask.HasFlag(RollDice.RollMask.Explosion);
                    var strike = r.Mask.HasFlag(RollDice.RollMask.Dropped);
                    var bold = (r.Mask & (RollDice.RollMask.Minimal | RollDice.RollMask.Maximun)) != 0;

                    if (under)
                        sb.AppendUnderline();
                    if (strike)
                        sb.AppendStrikethrough();
                    if (bold)
                        sb.AppendBold();

                    sb.Append(r.Roll);

                    if (bold)
                        sb.AppendBold();
                    if (strike)
                        sb.AppendStrikethrough();
                    if (under)
                        sb.AppendUnderline();

                    sb.Append(sep);
                }
                sb.Length -= sep.Length;
            }
            sb.CloseMask().AppendLine();

            sb.Append($"` {modifiers.Sum} `").AppendSpace();
            sb.OpenMask();
            if (modifiers.Modifiers.Count > 0)
            {
                foreach (var m in modifiers.AsString())
                    sb.Append(m).Append(sep);
                sb.Length -= sep.Length;
            }
            sb.CloseMask().AppendLine();

            if (shift.HasValue)
                sb.Append("Shift: ").Append(final - shift).AppendLine();

            sb.AppendLine("----------------");
        }

        return sb.ToString();
    }


}