using System.Text.Json;

namespace DiscordDiceRoller;

public class EnvItem
{
    public required string BotToken { get; set; }
}

public class EnvReader<T>
{
    public T Env { get; }
    public EnvReader(string filePath = "config.json", JsonSerializerOptions? options = null)
    {
        using FileStream stream = File.OpenRead(filePath);
        Env = JsonSerializer.Deserialize<T>(stream, options ?? null) ?? throw new Exception("Cant read config");
    }
    public EnvReader(T Env)
    {
        this.Env = Env;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(Env);
    }

}
