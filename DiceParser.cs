using System.Text.RegularExpressions;

namespace DiscordDiceRoller;

public static partial class DiceParser
{

    public const string ROLL_HELP = @"```md
## Other
    rh = send this message

## Roll format: 
    `{}d{} +{} -{} !{} ?{} t{} &{}`
    `use , for multiple rolls at same time`

### Multiples
    + = positive modifier
    - = negative modifier
    d/D = dice modifier (default 1d20)

### Single
    ! = minimal dice roll for explode (default 20)
    ? = max explosion (default 1, max is 99)
    t/T = number of rolls (default 1)
    & = against or shift from (default 0)

### TODO
    l = drop lowest X
    h = drop highest X
    k = keep highest X
    r = keep(retain) lowest X

```";

    public static string SingleDiceParse(int sides) => $"` {new Dice(sides).Roll()} `";

    public static string RollParse(ReadOnlySpan<char> command)
    {
        var cmd = CommandExpressionClean().Replace(command.ToString().ToLowerInvariant(), "").AsSpan();
        var commands = ModifiersRegex().EnumerateMatches(cmd);

        var roll = new Roll();

        foreach (var c in commands)
        {
            var current = cmd.Slice(c.Index, c.Length);
            var first = current[0];
            var after = cmd.Slice(c.Index + 1, c.Length - 1);

            if (first == 'd' || char.IsAsciiDigit(first))
            {
                // XdY, dY
                bool isNegative = first == '-';
                if (isNegative) // -XdY, -dY
                    first = current[0];

                uint amount = 1;
                int sides;

                if (first == 'd')
                {
                    sides = int.Parse(after);
                }
                else if (char.IsAsciiDigit(first))
                {
                    var split = after.ToString().Split('d');
                    amount = uint.Parse(split[0]);
                    sides = int.Parse(split[1]);
                }
                else
                    return "error: invalid dice: " + current.ToString();

                Dice dice = new(sides, 1, isNegative);
                DiceGroup droll = new(dice, amount);
                roll.dices.Add(droll);
            }
            else if (int.TryParse(after, out var result)) //()X
            {
                if (first == '-')
                {
                    roll.modifiers.Add(-result);
                }
                else if (first == '+')
                {
                    roll.modifiers.Add(result);
                }
                else if (first == '&')
                {
                    roll.shiftFrom = result;
                }
                else if (first == 't')
                {
                    roll.times = (uint)result;
                }
                else if (first == '!')
                {
                    roll.explodeOn = result;
                }
                else if (first == '?')
                {
                    roll.explosionTimes = (uint)result;
                }
                else
                    return "error: invalid first: " + first.ToString();
            }
            else
                return "error: invalid command: " + current.ToString();
        }

        return roll.CalculateString().ToString();
    }

    [GeneratedRegex(@"[^0-9\-+!?&dt]", RegexOptions.CultureInvariant)]
    private static partial Regex CommandExpressionClean();

    [GeneratedRegex(@"-?\d*d\d+|[+\-!?&t]\d+(?!d\d)", RegexOptions.CultureInvariant)]
    private static partial Regex ModifiersRegex();
}
