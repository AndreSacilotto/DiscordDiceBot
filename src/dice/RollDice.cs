using System.Text;

namespace DiscordDiceRoller;

public class RollModifier
{
    public const string INT_PLUS = "+0;-0;0";

    private int sum = 0;
    private readonly List<int> modifiers = new();
    public int Sum => sum;
    public List<int> Modifiers => modifiers;
    public void Add(int value)
    {
        sum += value;
        modifiers.Add(value);
    }

    public IEnumerable<string> AsString()
    {
        foreach (var item in modifiers)
            yield return item.ToString(INT_PLUS);
    }

}

public class RollDice
{
    public DiceGroup diceGroup;

    public bool canExplode;
    public int explodeOn;
    public int explosionTimes;

    public int dropHigh;
    public int dropLow;

    public RollDice(DiceGroup diceGroup, int explodeOn, int explosionTimes, int dropHigh = 0, int dropLow = 0)
    {
        canExplode = true;
        this.explodeOn = explodeOn;
        this.explosionTimes = explosionTimes;

        this.diceGroup = diceGroup;
        this.dropHigh = dropHigh;
        this.dropLow = dropLow;
    }

    public RollDice(DiceGroup diceGroup, int dropHigh = 0, int dropLow = 0)
    {
        canExplode = false;
        this.diceGroup = diceGroup;
        this.dropHigh = dropHigh;
        this.dropLow = dropLow;
    }

    public RollDice DeepCopy()
    {
        return (RollDice)MemberwiseClone();
    }

    public enum RollMask
    {
        None = 0,
        Maximun = 1 << 0,
        Minimal = 1 << 1,
        Explosion = 1 << 2,
        Dropped = 1 << 3,
    }

    public readonly record struct SingleRoll(int Roll, RollMask Mask = RollMask.None);

    public List<SingleRoll> CalculateRolls()
    {
        var list = new List<SingleRoll>(diceGroup.amount);
        var dice = diceGroup.dice;
        for (var j = 0; j < diceGroup.amount; j++)
        {
            var flags = RollMask.None;
            var roll = dice.Roll();
            if (roll == dice.max) flags |= RollMask.Maximun;
            if (roll == dice.min) flags |= RollMask.Minimal;
            list.Add(new(roll, flags));

            if (canExplode)
                for (int e = 0; e < explosionTimes; e++)
                {
                    if (roll >= explodeOn)
                    {
                        flags = RollMask.Explosion;
                        roll = dice.Roll();
                        if (roll == dice.max) flags |= RollMask.Maximun;
                        if (roll == dice.min) flags |= RollMask.Minimal;
                        list.Add(new(roll, flags));
                    }
                    else
                        break;
                }
        }

        list.Sort((x, y) => x.Roll.CompareTo(y.Roll));

        for (int i = list.Count - 1; i >= list.Count - dropHigh; i--)
        {
            var old = list[i];
            list[i] = new(old.Roll, old.Mask | RollMask.Dropped);
        }

        for (int i = 0; i < Math.Min(dropLow, list.Count); i++)
        {
            var old = list[i];
            list[i] = new(old.Roll, old.Mask | RollMask.Dropped);
        }

        return list;
    }

}