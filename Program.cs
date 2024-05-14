namespace DiscordDiceRoller;

internal class Program
{
    private static void Main(string[] args)
    {
        new DiscordEntryPoint().MainAsync().GetAwaiter().GetResult();
    }
}
