namespace Noah.Models;

public class Conversation
{
    public string ConvId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ParticipantUserIds { get; set; }
    public string? LastMsgText { get; set; }
    public long? LastMsgAt { get; set; }
    public int? MsgCount { get; set; }
    public long? FileSize { get; set; }
    public long CreatedAt { get; set; }
    public long? LastOpenedAt { get; set; }
}
