namespace enx_fit.Models;

public sealed class ProgressPhoto
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
}
