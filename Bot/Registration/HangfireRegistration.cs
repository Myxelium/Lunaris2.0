using Hangfire;

namespace Lunaris2.Registration;

public static class HangfireRegistration
{
    public static IApplicationBuilder UseHangfireDashboardAndServer(this IApplicationBuilder app, string dashboardPath = "/hangfire")
    {
        var dashboardOptions = new DashboardOptions
        {
            DarkModeEnabled = true,
            DashboardTitle = "Lunaris Jobs Dashboard"
        };
        
        app.UseHangfireDashboard(dashboardPath, dashboardOptions);

        return app;
    }
}