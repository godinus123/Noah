namespace Noah.Models;

public class Message
{
    public string MsgId { get; set; } = string.Empty;
    public long? ServerSeq { get; set; }
    public string FromUserId { get; set; } = string.Empty;
    public string? FromUsername { get; set; }
    public string? Text { get; set; }
    public int HasAttachment { get; set; }
    public long Timestamp { get; set; }
    public int IsOutgoing { get; set; }
    public int IsAi { get; set; }
    public string Status { get; set; } = "sending";
    public long CreatedAt { get; set; }
}
