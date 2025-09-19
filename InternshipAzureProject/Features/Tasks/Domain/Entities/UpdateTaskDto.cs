namespace Domain.Entities.Dtos
{
    public class UpdateTaskDto
    {
        public string? Title { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int? PercentComplete { get; set; }
        public List<string>? UsersToAssign { get; set; }
    }
}
