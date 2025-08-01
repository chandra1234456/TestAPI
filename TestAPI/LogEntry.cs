public class LogEntry
{
    public int Id { get; set; }

    public string FileName { get; set; }   // Must NOT be null in DB unless allowed

    public string Content { get; set; }    // Same here
    public string DeviceInfo { get; set; }    // Same here
    public string ExceptionType { get; set; }    // Same here
    public long FileSize { get; set; }    // Same here

    public DateTime CreatedAt { get; set; }
}
