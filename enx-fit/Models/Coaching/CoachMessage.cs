namespace enx_fit.Models;

public sealed class CoachMessage
{
    public int Id { get; set; }
    public string SenderId { get; set; } = "";
    public string RecipientId { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
    public Guid RequestId { get; set; }
}
