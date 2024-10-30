namespace Asterix_Log_Analyzer.Chart;


public class ChartInfo
{
    public List<string>? xCategories { get; set; }// = new string[0];
    public List<string>? yCategories { get; set; } // = { "0", "1", "2", "3" };

    public int Width { get; set; } = 1500;
    public int Height { get; set; } = 1000;
    public int Margin { get; set; } = 100;
    public string TimeStart { get; internal set; }
    public string TimeEnd { get; internal set; }
}
