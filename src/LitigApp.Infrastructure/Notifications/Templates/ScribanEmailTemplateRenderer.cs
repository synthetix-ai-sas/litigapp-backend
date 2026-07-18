using System.Collections.Concurrent;
using LitigApp.Application.Common.Abstractions;
using Scriban;

namespace LitigApp.Infrastructure.Notifications.Templates;

/// <summary>
/// Loads the 3 email templates as Embedded Resources (robust in Docker/Railway — no
/// dependency on the .html files existing on disk at runtime) and renders them with
/// Scriban. Parsed templates are cached by <see cref="EmailTemplate"/> so repeated
/// sends (e.g. the fallback sweep) don't re-parse the markup.
/// </summary>
internal sealed class ScribanEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Dictionary<EmailTemplate, string> FileNames = new()
    {
        [EmailTemplate.UserDigest] = "UserDigestTemplate.html",
        [EmailTemplate.ImportComplete] = "ImportCompleteTemplate.html",
        [EmailTemplate.PasswordReset] = "PasswordResetTemplate.html",
    };

    private readonly ConcurrentDictionary<EmailTemplate, Template> _cache = new();

    public string Render(EmailTemplate template, IReadOnlyDictionary<string, object?> model)
    {
        var parsed = _cache.GetOrAdd(template, LoadAndParse);
        return parsed.Render(model);
    }

    private static Template LoadAndParse(EmailTemplate template)
    {
        var fileName = FileNames[template];
        var assembly = typeof(ScribanEmailTemplateRenderer).Assembly;

        // Matched by filename suffix rather than the full logical resource name — robust
        // against RootNamespace/folder drift, matching the same assembly regardless of
        // how MSBuild computed the embedded resource's manifest name.
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Embedded resource for '{fileName}' not found in {assembly.FullName}.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var parsed = Template.Parse(text, fileName);
        if (parsed.HasErrors)
        {
            throw new InvalidOperationException(
                $"Failed to parse email template '{fileName}': " +
                string.Join("; ", parsed.Messages));
        }

        return parsed;
    }
}
