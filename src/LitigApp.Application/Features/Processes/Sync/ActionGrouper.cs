using System.Globalization;
using System.Text;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Features.Processes.Sync;

/// <summary>
/// Links each "Fijación de estado" action to the "Auto" it notifies: the immediately
/// preceding Auto (highest consecutive below the Fijación's) recorded on the same
/// fechaRegistro (<see cref="ProcessAction.RecordedAt"/>). Heuristic per blueprint schema
/// (recorded_at = grouping key, is_grouped_with = self-ref to the preceding Auto).
/// Pure and side-effect-free except for setting <see cref="ProcessAction.GroupedWithId"/>
/// on the Fijación rows in <paramref name="newActions"/>.
/// </summary>
public static class ActionGrouper
{
    public static void AssignGroups(
        IReadOnlyCollection<ProcessAction> newActions,
        IReadOnlyCollection<ProcessAction> priorActions)
    {
        // Candidate Autos come from both this run's new actions and already-persisted ones.
        var autos = newActions.Concat(priorActions).Where(IsAuto).ToList();
        if (autos.Count == 0)
            return;

        foreach (var fijacion in newActions.Where(IsFijacion))
        {
            var auto = autos
                .Where(a => a.RecordedAt is not null
                            && a.RecordedAt == fijacion.RecordedAt
                            && a.ConsecutiveNumber < fijacion.ConsecutiveNumber)
                .OrderByDescending(a => a.ConsecutiveNumber)
                .FirstOrDefault();

            if (auto is not null)
                fijacion.GroupedWithId = auto.Id;
        }
    }

    private static bool IsFijacion(ProcessAction a) => NormalizedContains(a.Action, "fijacion");

    private static bool IsAuto(ProcessAction a) => NormalizedContains(a.Action, "auto");

    private static bool NormalizedContains(string? text, string token) =>
        Normalize(text).Contains(token, StringComparison.Ordinal);

    /// <summary>Lowercases and strips accents so "Fijación" matches "fijacion".</summary>
    private static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }
}
