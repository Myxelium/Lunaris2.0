using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lunaris2.Registration;

var builder = WebApplication.CreateBuilder(args);

// Build configuration (using appsettings.json)
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

// Register your services
builder.Services.AddDiscordBot(configuration);
builder.Services.AddScheduler(configuration);
builder.Services.AddControllers();

var app = builder.Build();

// Call your custom middleware (e.g., for SponsorBlock functionality)
app.UseSponsorBlock();

// Serve static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHangfireDashboardAndServer();
app.Run();