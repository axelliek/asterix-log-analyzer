namespace AsterixLogAnalyzer.Domain;

/**
 * <summary>Represents LogEntry</summary>
 */
public class LogEntry
{
    public required string Timestamp { get; set; }
    public required string CallID { get; set; }
    public required string Queue { get; set; }
    public string? Channel { get; set; }
    public required string Event { get; set; }
    public string? Param1 { get; set; }
    public string? Param2 { get; set; }
    public string? Param3 { get; set; }
    public override string ToString()
    {
        return $"{CallID}\t{Event}";
    }
}
