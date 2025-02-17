using Lunaris2.Handler.ChatCommand;

namespace Lunaris2.Registration;

public static class ChatRegistration
{
    public static IServiceCollection AddChat(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ChatSettings>();
        services.Configure<ChatSettings>(configuration.GetSection("LLM"));
        
        return services;
    }
}