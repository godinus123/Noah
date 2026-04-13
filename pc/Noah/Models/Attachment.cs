namespace Noah.Models;

public class Attachment
{
    public string AttachmentId { get; set; } = string.Empty;
    public string MsgId { get; set; } = string.Empty;
    public string? Filename { get; set; }
    public string? Mime { get; set; }
    public long? Size { get; set; }
    public byte[]? Data { get; set; }
    public long CreatedAt { get; set; }
}
