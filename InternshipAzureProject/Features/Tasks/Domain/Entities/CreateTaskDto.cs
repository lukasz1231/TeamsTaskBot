namespace Domain.Entities.Dtos
{
    public class CreateTaskDto
    {
        public string Title { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
    }
}
