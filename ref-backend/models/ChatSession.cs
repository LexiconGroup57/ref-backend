namespace ref_backend.models;

public class ChatSession
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string MessageHistory { get; set; }
}