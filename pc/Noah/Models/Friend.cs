namespace Noah.Models;

public class Friend
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? StatusMessage { get; set; }
    public long? LastActive { get; set; }
}
