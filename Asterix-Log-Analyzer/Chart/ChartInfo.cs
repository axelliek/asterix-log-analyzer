namespace Asterix_Log_Analyzer.Chart;


public class ChartInfo
{
    public List<string>? XCategories { get; set; }
    public List<string>? YCategories { get; set; }
    public List<List<StackedBarValues>>? Values { get; set; } 
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public int Margin { get; set; } = 50;
    public string? TimeStart { get; internal set; }
    public string? TimeEnd { get; internal set; }
    public long StartTime { get; internal set; }
    public long EndTime { get; internal set; }

}
