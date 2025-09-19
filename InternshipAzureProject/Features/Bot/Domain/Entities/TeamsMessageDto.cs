namespace Domain.Entities.Dtos
{
    public class TeamsMessageDto
    {
        public string Id { get; set; }
        public string? Title { get; set; }
        public string FromUserId { get; set; }
        public string? ReplyToId { get; set; }
        public string FromDisplayName { get; set; }
        public string Body { get; set; }
        public DateTimeOffset? CreatedDateTime { get; set; }
    }
}
