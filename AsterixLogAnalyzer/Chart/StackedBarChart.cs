using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace AsterixLogAnalyzer.Chart;

/**
 * <summary>Represents StackedBarChart</summary>
 */
public class StackedBarChart
{
    private const int DefaultYOffsetLabelPosition = 20;
    private const int DefaultFontsize = 8;
    private const string DefaultFontFamily = "Arial";
    private const int DefaultBarXMargin = 0;

    public string FontFamily { get; set; } = DefaultFontFamily;
    public float FontSize { get; set; } = DefaultFontsize;
    public int YOffsetLabelPosition { get; set; } = DefaultYOffsetLabelPosition;

    private Bitmap? _bitmap = null;

    public Bitmap? Bitmap { get => _bitmap; }
    private readonly ChartInfo? _chartInfo;

    public StackedBarChart(ChartInfo? chartInfo)
    {
        _chartInfo = chartInfo;
        CreateChartBitmap(/*_chartInfo*/);
    }

    /**
     * <summary>Generates chart bitmap</summary>
     */
    private bool CreateChartBitmap()
    {
        if (_chartInfo == null)
            throw new ArgumentNullException(nameof(_chartInfo));
#pragma warning disable CA1416
        int width = _chartInfo.Width;
        int height = _chartInfo.Height;
        int margin = _chartInfo.Margin;

        using var currentFont = new System.Drawing.Font(FontFamily, FontSize);
        _bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(_bitmap);
        g.Clear(Color.LightGray);
        //g.Clear(Color.White);
        var xAxisTop = margin;

        // Draw the axes
        g.DrawLine(Pens.Black, margin, height - margin, margin, margin); // Y-axis
        g.DrawLine(Pens.Black, margin, height - margin, width - margin, height - margin); // X-axis

        string[] xAxisLabels = [.. _chartInfo.XCategories!];
        float step = (float)(width - 2 * margin) / (xAxisLabels.Length - 1);

        float labelYPos = height - margin + YOffsetLabelPosition;
        float xStartPosition = margin;
        float labelXPos = xStartPosition;

        for (int i = 0; i < xAxisLabels.Length; i++, labelXPos += step)
        {
            SizeF size = g.MeasureString(xAxisLabels[i], currentFont);
            g.DrawLine(Pens.Black, labelXPos, height - margin, labelXPos, height - margin + 10);
            g.DrawString(xAxisLabels[i], currentFont, Brushes.Black, labelXPos - size.Width / 2, labelYPos); // Label
        }

        string[] yAxisLabels = [.. _chartInfo.YCategories!];
        var barHeight = (height - 2 * margin) / (yAxisLabels.Length);

        var logTimeDuration = _chartInfo.EndTime - _chartInfo.StartTime;
        var chartWidth = (width - 2 * (margin + DefaultBarXMargin));

        float timeToPixelScaleFactor = (logTimeDuration) / (float)(chartWidth);

        var xAxisLeft = margin + DefaultBarXMargin;
        var xAxisBottom = height - margin;

        //DrawBars
        for (var i = 0; i < _chartInfo.Values!.Count; i++)
        {
            for (var j = 0; j < _chartInfo.Values[i].Count; j++)
            {
                var barValue = _chartInfo.Values[i][j];

                var barLeft = xAxisLeft + (barValue.Start - _chartInfo.StartTime) / timeToPixelScaleFactor;
                var barWidth = (barValue.Wait + barValue.Speak) / timeToPixelScaleFactor;

                var bHeightScale = barHeight / (barValue.Wait + barValue.Speak);

                var barHeightSpeak = barValue.Speak * bHeightScale;
                var barTop = xAxisBottom - (barHeight * (i + 1));

                // Draw call start time tick
                g.DrawLine(Pens.Black, (float)barLeft, (float)xAxisBottom, (float)barLeft, (float)(xAxisBottom + 5));

                // Draw the bar
                g.FillRectangle(Brushes.Green, (float)(barLeft), barTop, (float)(barWidth), barHeightSpeak);

                var brush = (barValue.Status == "ABANDON" || barValue.Status == "WAITED")
                    ? Brushes.Red : Brushes.Yellow;
                g.FillRectangle(brush,
                    (float)barLeft, barTop + barHeightSpeak,
                    (float)barWidth, barHeight - barHeightSpeak);

            }
        }


        var barTextYMargin = barHeight;
        var barXOffset = 30;
        var labelXPosition = margin - barXOffset;
        // Draw y labels
        for (var i = 0; i < yAxisLabels.Length; i++)
        {
            // y axis label text position

            var labelYPosition = height - margin - ((i + 1) * barTextYMargin);

            g.DrawString(yAxisLabels[i], currentFont, Brushes.Black, labelXPosition, labelYPosition);
        }

#pragma warning restore CA1416
        return true;
    }
}


