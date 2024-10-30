using System.Drawing;
using System.Diagnostics;
using Asterix_Log_Analyzer.Chart;

namespace Asterix_Log_Analyzer;

partial class Program
{
    public class Chart
    {
#pragma warning disable CA1416
        // The code that's violating the rule is on this line.
        public static bool CreateChartBitmap(string imagePath, ChartInfo chartInfo)
        {
            int width = chartInfo.Width;
            int height = chartInfo.Height;
            int margin = chartInfo.Margin;

            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);

            var xAxisXOffset = margin;
            var xAxisYOffset = height - margin;
            g.Clear(Color.White);

            // Draw the axes
            g.DrawLine(Pens.Black, margin, height - margin, margin, margin); // Y-axis
            g.DrawLine(Pens.Black, margin, height - margin, width - margin, height - margin); // X-axis

            string[] xCategories = [.. chartInfo.XCategories!];

            string[] yCategories = [.. chartInfo.YCategories!];

            using var current = new System.Drawing.Font("Arial", 8);
            var step = (width - margin) / xCategories.Length;

            for (var i = 0; i < xCategories.Length; i++)
            {
                var x = margin + step * i; // Position of the bar
                var y = height - margin + 20;

                Debug.WriteLine($"DrawString {xCategories[i]} pos: (x:{x}, y:{y})");

                g.DrawString(xCategories[i], current, Brushes.Black, x, y); // Label
            }

            var barHight = (height - 2 * margin) / (yCategories.Length);


            var Scale = (chartInfo.EndTime - chartInfo.StartTime) / (width - 2 * margin);
            for (var i = 0; i < chartInfo.Values!.Count; i++)
            {
                for (var j = 0; j < chartInfo.Values[i].Count; j++)
                {
                    var bb = chartInfo.Values[i][j];

                    var xcS = (bb.Start - chartInfo.StartTime) / Scale;
                    var xcE = (bb.End - chartInfo.EndTime);

                    var waitWidth = bb.Wait / Scale;
                    var waitStart = xcS;
                    var speakStart = waitStart + waitWidth;
                    var speakWidth = bb.Speak / Scale;
                    Debug.WriteLine($"{xcS} {bb.Status} {waitStart} {bb.Start - chartInfo.StartTime} {bb.Wait} {bb.Speak}");
                    Debug.WriteLine($"{i} {j} {xAxisXOffset + speakStart}, {xAxisYOffset - (barHight * (i + 1))}, {speakWidth} {barHight}");

                    g.FillRectangle(Brushes.Green, xAxisXOffset + speakStart, xAxisYOffset - (barHight * (i + 1)), speakWidth, barHight); // Draw the bar
                    if (bb.Status == "ABANDON" || bb.Status == "WAITED")
                    {
                        g.FillRectangle(Brushes.Red, xAxisXOffset + waitStart, xAxisYOffset - (barHight * (i + 1)), waitWidth, barHight); // Draw the bar
                    }
                    else
                    {
                        g.FillRectangle(Brushes.Yellow, xAxisXOffset + waitStart, xAxisYOffset - (barHight * (i + 1)), waitWidth, barHight); // Draw the bar
                    }
                }
            }


            var barTextYMargin = barHight;
            var barXOffset = 30;
            var ySlots = barHight * yCategories.Length;

            // Draw y labels
            for (var i = 0; i < yCategories.Length; i++)
            {
                // y axis label text position
                var xText = margin - barXOffset;
                var yText = height - margin - ((i + 1) * barTextYMargin);

                Debug.WriteLine($"DrawString {yCategories[i]} pos: ( x:{xText}, y:{yText} )");

                g.DrawString(yCategories[i], current, Brushes.Black, xText, yText);
            }
            File.Delete(imagePath);
            // Save the chart as BMP
            bmp.Save(imagePath);
            return true;
        }
#pragma warning restore CA1416
    }
}
