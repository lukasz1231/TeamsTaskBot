namespace Domain.Interfaces
{
    public interface ISyncService
    {
        public Task SyncUsersAsync();
        public Task SyncTasksToPlannerAsync();
    }
}
