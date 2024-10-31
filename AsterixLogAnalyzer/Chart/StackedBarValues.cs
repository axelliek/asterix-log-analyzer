namespace AsterixLogAnalyzer.Chart;

public class StackedBarValues(long start, long end, long wait, long speak, string status)
{
    public long Start { get; set; } = start;
    public long End { get; set; } = end;
    public long Wait { get; set; } = wait;
    public long Speak { get; set; } = speak;
    public string? Status { get; set; } = status;

    public StackedBarValues() : this(0L, 0L, 0L, 0L, string.Empty) { }

    public bool Intersect(StackedBarValues call) => !(Start >= call.End);
   
}