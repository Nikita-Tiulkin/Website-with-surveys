namespace PollingSystem.Data.Entities;

public class WorkerLease
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
