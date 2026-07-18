namespace LitigApp.Application.Common.Abstractions;

/// <summary>The 3 email templates shipped as embedded HTML resources (blueprint §10.4).</summary>
public enum EmailTemplate
{
    UserDigest,
    ImportComplete,
    PasswordReset,
}

/// <summary>
/// Renders one of the embedded HTML templates against a model. Implementations own the
/// template engine (Scriban) and HTML-escaping is expected to already be applied to any
/// free-text value in <paramref name="model"/> before it reaches this call.
/// </summary>
public interface IEmailTemplateRenderer
{
    string Render(EmailTemplate template, IReadOnlyDictionary<string, object?> model);
}
