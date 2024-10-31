namespace AsterixLogAnalyzer.Chart;


public class ChartInfo
{
    public List<string>? XCategories { get; set; }
    public List<string>? YCategories { get; set; }
    public List<List<StackedBarValues>>? Values { get; set; }
    public int Width { get; set; } = 1500;
    public int Height { get; set; } = 1000;
    public int Margin { get; set; } = 100;
    public string? TimeStart { get; internal set; }
    public string? TimeEnd { get; internal set; }
    public long StartTime { get; internal set; }
    public long EndTime { get; internal set; }

}
