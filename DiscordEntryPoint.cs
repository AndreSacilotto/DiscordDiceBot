using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordDiceRoller;

public class DiscordEntryPoint
{
    private readonly DiscordSocketClient client;

    public DiscordEntryPoint()
    {
        client = new DiscordSocketClient(new(){ 
            MessageCacheSize = 0,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            LogLevel = LogSeverity.Warning,
        });
    }

    public async Task MainAsync()
    {
        var env = new EnvReader<EnvItem>();

        await client.LoginAsync(TokenType.Bot, env.Env.BotToken);
        await client.StartAsync();

        client.Ready += () =>
        {
            Console.WriteLine("Bot is connected: " + env.Env.BotToken);
            return Task.CompletedTask;
        };
        client.MessageReceived += MessageReceived;
        //client.MessageUpdated += MessageUpdated;

        await Task.Delay(-1); // Infite Delay
    }
    
    //private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    //{
    //    var message = await before.GetOrDownloadAsync();
    //    Console.WriteLine($"[Updated]: {message.Content} -> {after.Content}");
    //}

    private async Task MessageReceived(SocketMessage message)
    {

        if (message.Source != MessageSource.User)
            return;

        var response = Command(message.Content.AsSpan());
        if (response.ValidCommand)
        {
            MessageReference? mr = response.Reply ? new(message.Id) : null;

            await message.Channel.SendMessageAsync(response.Message, messageReference: mr);

            if (response.DeleteSource)
                await message.Channel.DeleteMessageAsync(message.Id);
        }
        else
            await message.Channel.SendMessageAsync("invalid command", messageReference: new(message.Id));
    }

    private readonly record struct CommandResponse(bool ValidCommand, string Message, bool Reply = true, bool DeleteSource = false)
    {
        public static CommandResponse Invalid => new(true, string.Empty);
    }

    private static CommandResponse Command(ReadOnlySpan<char> command)
    {
        const char commandKey = 'r';

        if (command.Length == 0 || command[0] != commandKey)
            return CommandResponse.Invalid;

        Console.WriteLine($"[Received]: {command}");

        if (command.Length == 2)
        {
            Console.WriteLine("1");
            switch (command[1]) // help
            {
                case 'h':
                return new(true, DiceParser.ROLL_HELP, true);
                case 'r':
                return new(true, DiceParser.SingleDiceParse(Dice.rng.Next()), true);
            }
        }
        else if (command.Length > 2)
        {
            Console.WriteLine("2");
            var response = DiceParser.RollParse(command.Slice(1));
            return new(true, response, true);
        }

        return CommandResponse.Invalid;
    }


}