using Hangfire.Dashboard;

namespace LitigApp.Api.Hangfire;

public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In Development, allow local requests without auth so devs can inspect the dashboard.
        // The Hangfire dashboard relies on browser navigation (no Authorization header),
        // so JWT Bearer auth never populates httpContext.User for it.
        var env = httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
            return true;

        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole("Admin");
    }
}
