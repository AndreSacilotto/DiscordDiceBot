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

    public int explodeOn;
    public int explosionTimes;

    public int dropHigh;
    public int dropLow;

    public bool CanExplode() => explosionTimes >= 0;

    public RollDice(DiceGroup diceGroup, int explodeOn, int explosionTimes, int dropHigh = 0, int dropLow = 0)
    {
        this.explodeOn = explodeOn;
        this.explosionTimes = explosionTimes;

        this.diceGroup = diceGroup;
        this.dropHigh = dropHigh;
        this.dropLow = dropLow;
    }

    public RollDice(DiceGroup diceGroup, int dropHigh = 0, int dropLow = 0)
    {
        explosionTimes = -1;
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
        Explosion = 1 << 2, // dice from explosion
        Exploded = 1 << 3, // dice that exploded
        Dropped = 1 << 4,
    }

    public readonly record struct SingleRoll(int Roll, RollMask Mask = RollMask.None);

    public List<SingleRoll> CalculateRolls(out int sum)
    {
        var list = new List<SingleRoll>(diceGroup.amount);
        var dice = diceGroup.dice;
        var canExplode = CanExplode();
        for (var j = 0; j < diceGroup.amount; j++)
        {
            var flags = RollMask.None;
            var roll = dice.Roll();
            if (roll == dice.max) flags |= RollMask.Maximun;
            if (roll == dice.min) flags |= RollMask.Minimal;

            if (canExplode && roll >= explodeOn)
            {
                flags |= RollMask.Exploded;
                list.Add(new(roll, flags));
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
            else
                list.Add(new(roll, flags));
        }

        list.Sort((x, y) => x.Roll.CompareTo(y.Roll));

        // l1 h2 [2 4 8 16 32]_5 (-2) = 3
        // 0:2 1:4 2:8 4:16 5:32

        sum = 0;
        var h = list.Count - dropHigh - 1;
        int explosionToDelete = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var old = list[i];
            if (i < dropLow || i > h)
            {
                if (old.Mask.HasFlag(RollMask.Exploded))
                    explosionToDelete++;
                list[i] = new(old.Roll, old.Mask | RollMask.Dropped);
            }
            else
                sum += old.Roll;
        }

        if (explosionToDelete > 0)
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var old = list[i];
                if (list[i].Mask.HasFlag(RollMask.Explosion))
                {
                    sum -= old.Roll;
                    list[i] = new(old.Roll, old.Mask | RollMask.Dropped);
                    if (--explosionToDelete == 0)
                        break;
                }
            }

        return list;
    }

}