namespace LitigApp.Domain.Processes;

public static class ProcessSyncStatus
{
    public const string Pending   = "pending";
    public const string Partial   = "partial";
    public const string Ok        = "ok";
    public const string Error     = "error";
    public const string NotFound  = "not_found";
}
