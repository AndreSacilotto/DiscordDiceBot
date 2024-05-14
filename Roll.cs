using System.Text;

namespace DiscordDiceRoller;

public class Roll
{
    public const string INT_PLUS = "+0;-0;0";

    public List<int> modifiers = new(1);
    public List<DiceGroup> dices = new(1);
    public int explodeOn = 20;
    public uint explosionTimes = 1;
    public uint times = 1;
    public int? shiftFrom = null;

    public int[] Calculate()
    {
        int modSum = modifiers.Sum();
        var rollsArray = new int[times];
        for (int i = 0; i < rollsArray.Length; i++)
        {
            rollsArray[i] = modSum;
            foreach (var group in dices)
            {
                var d = group.dice;
                for (var e = group.amount; e >= 0; e--)
                {
                    int roll = d.Roll();
                    rollsArray[i] += roll;
                    if (roll < explodeOn)
                        e++;
                }
            }
        }

        if (rollsArray.Length > 1)
            Array.Sort(rollsArray);

        return rollsArray;
    }

    public readonly record struct SingleRoll(int Sum, string List)
    {
        public bool HasRolls() => string.IsNullOrEmpty(List);
    }

    public StringBuilder CalculateString()
    {
        StringBuilder sb = new();
        var sep = ", ".AsSpan();

        // Modifier Calculation
        int modSum = 0;
        var modList = ReadOnlySpan<char>.Empty;
        if (modifiers.Count > 0)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                modSum += modifiers[i];
                sb.Append(modifiers[i].ToString(INT_PLUS));
                sb.Append(sep);
            }
            sb.Length -= sep.Length;
            modList = sb.Submit().AsSpan();
        }

        // Rolls Calculation
        var rollsCount = (int)times;
        var rollsArray = new SingleRoll[rollsCount];
        for (int i = 0; i < rollsCount; i++)
        {
            int rollSum = 0;
            foreach (var group in dices)
            {
                var d = group.dice;
                for (var e = group.amount; ;)
                {
                    int roll = d.Roll();
                    if (roll != d.max && roll != d.min) sb.Append(roll);
                    else sb.AppendBold().Append(roll).AppendBold();

                    rollSum += roll;
                    if (roll >= explodeOn)
                        e++;
                    else if (--e == 0)
                        break;
                    sb.Append(sep);
                }
            }
            rollsArray[i] = new(rollSum, sb.Submit());
        }

        if (rollsArray.Length > 1)
            Array.Sort(rollsArray, (x, y) => x.Sum.CompareTo(y.Sum));

        // Format Roll+Modifier
        foreach (var roll in rollsArray)
        {
            var final = roll.Sum + modSum;

            sb.Append($"` {final} `");

            // rolls result
            if (roll.HasRolls())
            {
                sb.AppendLine();
                sb.Append(roll.Sum).AppendSpace().OpenMask().Append(roll.List).CloseMask();
            }

            // modifier
            if (modifiers.Count > 0)
            {
                sb.AppendLine();
                sb.Append(modSum).AppendSpace().OpenMask().Append(modList).CloseMask();
            }

            // shift
            if (shiftFrom.HasValue)
            {
                var shift = final - shiftFrom.Value;
                sb.Append($" | Shift ({shiftFrom.Value}): {shift.ToString(INT_PLUS)}");
            }

            // next
            sb.AppendLine();
        }

        return sb;
    }


}