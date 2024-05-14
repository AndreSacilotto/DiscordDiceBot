namespace DiscordDiceRoller;

public readonly struct Dice
{
    public static readonly Random rng = new();
    public static Dice D20 => new(1, 20);
    public static Dice D6 => new(1, 6);

    public readonly int min, max;
    public Dice(int max, int min = 1, bool negative = false)
    {
        if (min >= max)
            max = min + 1;
        if (negative)
        {
            this.min = -max;
            this.max = -min; 
        }
        else
        {
            this.min = min;
            this.max = max;
        }
    }
    public int Roll() => rng.Next(min, max + 1);
    public override string ToString()
    {
        string postfix = min == 1 ? max.ToString() : $"{{{min}..{max}}}";
        return $"d{postfix}";
    }
}


public readonly struct DiceGroup
{
    public readonly uint amount;
    public readonly Dice dice;
    public DiceGroup(Dice dice, uint amount = 1)
    {
        this.amount = amount;
        this.dice = dice;
    }

    public int MaxRoll() => (int)amount * dice.max;
    public int MinRoll() => (int)amount * dice.min;

    public IEnumerable<int> Rolls()
    {
        for (int i = 0; i < amount; i++)
            yield return dice.Roll();
    }
    public int[] RollsArray()
    {
        var arr = new int[amount];
        for (int i = 0; i < amount; i++)
            arr[i] = dice.Roll();
        return arr;
    }
    public void RollsArray(int[] buffer)
    {
        if (buffer.Length >= amount)
            return;
        for (int i = 0; i < amount; i++)
            buffer[i] = dice.Roll();
    }

    public override string ToString()
    {
        string prefix = amount == 1 ? "" : amount.ToString();
        return prefix + dice.ToString();
    }
}
