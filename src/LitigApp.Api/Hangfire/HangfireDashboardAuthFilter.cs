using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;

namespace LitigApp.Api.Hangfire;

public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In Development, allow local requests without auth so devs can inspect the dashboard.
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
            return true;

        // Basic Auth: the dashboard is plain browser navigation, so JWT bearer auth (which
        // needs an Authorization header the browser never attaches on its own) can't
        // protect it here. Password comes from Hangfire:DashboardPassword.
        var configuredPassword = httpContext.RequestServices
            .GetRequiredService<IOptions<HangfireOptions>>().Value.DashboardPassword;

        if (!string.IsNullOrEmpty(configuredPassword)
            && TryGetBasicAuthPassword(httpContext.Request, out var providedPassword)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedPassword), Encoding.UTF8.GetBytes(configuredPassword)))
        {
            return true;
        }

        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
        return false;
    }

    private static bool TryGetBasicAuthPassword(HttpRequest request, out string password)
    {
        password = "";
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex < 0)
                return false;

            password = decoded[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
