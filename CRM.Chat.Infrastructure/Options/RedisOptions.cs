namespace CRM.Chat.Infrastructure.Options;

public class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string? Password { get; set; }
    public int Database { get; set; } = 0;
}