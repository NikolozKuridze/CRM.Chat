namespace CRM.Chat.Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";
        
    public string DefaultConnection { get; set; } = string.Empty;
    public string ChatConnection { get; set; } = string.Empty;
    public string RedisConnection { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}