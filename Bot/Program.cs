using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lunaris2.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Lunaris2;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            Console.WriteLine(eventArgs.ExceptionObject);
        };
        var app = CreateHostBuilder(args).Build();
        
        app.UseSponsorBlock();
        app.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                services.AddDiscordBot(configuration);
            });
}