// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

//using ScottPlot;

using System;
using System.Data;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Diagnostics;

namespace Asterix_Log_Analyzer;
public class LogEntry
{
    public required DateTime Timestamp { get; set; }
    public required string CallID { get; set; }
    public required string Queue { get; set; }
    public string? Channel { get; set; }
    public required string Event { get; set; }
    public string? Param1 { get; set; }
    public string? Param2 { get; set; }
    public string? Param3 { get; set; }
    public override string ToString()
    {
        return $"{CallID}\t{Event}";// base.ToString();
    }
}


/**
 * <summary></summary>
 */
public class ProgramOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
}
class Program
{
    private const char LineSeparator = '|';

    static void Main(string[] args)
    {
        var programOptions = ProcessProgramArgs(args);

        string filePath = "Testdaten.txt"; // Anpassung erforderlich, wenn die Datei woanders liegt
        List<LogEntry>? data = null;
        // Daten einlesen (vereinfacht, anpassbar an das genaue Format)
        try
        {
            var text = File.ReadAllLines(filePath);
            data = text
                .Select(line => line.Split(LineSeparator))
                .Select(parts => new LogEntry()
                {
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(parts[0])).DateTime, // FromFileTime() ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", null),
                    CallID = parts[1],
                    Queue = parts[2],
                    Channel = parts[3],
                    Event = parts[4],
                    Param1 = parts.Length >= 6 ? parts[5] : string.Empty,
                    Param2 = parts.Length >= 7 ? parts[6] : string.Empty,
                    Param3 = parts.Length >= 8 ? parts[7] : string.Empty,
                    //Duration = int.Parse(parts[3]),
                    //Status = parts[4]
                })
                .Where(data => data.CallID != "NONE" && data.CallID != "NULL")
                //.GroupBy(data => data.CallID)
                .ToList();
            if (data == null || (data?.Count == 0)) throw new Exception("Data is null empty");
        }
        catch (FileNotFoundException fileException) { Console.WriteLine(fileException.Message); }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        // Eingehende Anrufe
        var d = data.GroupBy(x => x.CallID).ToList();
        // Eingeloggte Agenten
        var sst = data.GroupBy(x => x.Channel).Count();

        Console.WriteLine($"Eingeloggte Agenten {sst}");
        Console.WriteLine($"Abgeschlossene Anrufe {d.Count}");
        string[] find = ["ABANDON", /*"ENTERQUEUE", "CONNECT",*/ /*"RINGNOANSWER",*/ "COMPLETEAGENT", "AGENTDUMP", "COMPLETECALLER"/*, "ENTERQUEUE"*/];
        Console.WriteLine($"Eingehende Anrufe {d.Count}");
        Console.WriteLine($"{d[0]?.FirstOrDefault()?.CallID}\t{d[0]?.FirstOrDefault()?.Timestamp}");
        Console.WriteLine($"{d[^1]?.FirstOrDefault()?.CallID}\t{d[^1]?.FirstOrDefault()?.Timestamp}");
        //var vv = d.GroupBy(x => x.Event).Where(x => find.Contains(x.Key) /* == "ABANDON" || x.Key == "CONNECT"*/ ).ToList(); //.ForEach(x => x);
        //vv.ForEach(t => Console.WriteLine(GetInfo(t.FirstOrDefault())));
        int count = 0;
        d.ForEach(item =>
        {
            var vv = item.GroupBy(x => x.Event).Where(x => find.Contains(x.Key)).ToList();
            count += vv.Count;
            Console.WriteLine(GetInfo(vv.Count == 1 ? vv[0]?.FirstOrDefault() : null));
        });
        Console.WriteLine($"Abgeschlossene Anrufe {count}");
        //foreach (var item in d)
        //{
        //    var vv = item.GroupBy(x => x.Event).Where(x => find.Contains(x.Key) /* == "ABANDON" || x.Key == "CONNECT"*/ ).ToList(); //.ForEach(x => x);
        //    Console.WriteLine($"{vv.Count}");
        //    //vv.ForEach(t => Console.WriteLine(GetInfo(t.FirstOrDefault())));
        //    //vv.Firs
        //    //foreach (var s in vv)
        //    //{
        //    //    if (s.Key == "COMPLETECALLER" || s.Key == "COMPLETEAGENT")
        //    //        Console.WriteLine($"Wartezeit {s.First().Param1} Anrufdauer {s.First().Param2} Einstiegsposition {s.First().Param3}");
        //    //    //if (s.Key == "COMPLETEAGENT")
        //    //    //Console.WriteLine($"Wartezeit {s.First().Param1} Anrufdauer {s.First().Param2} Anrufdauer {s.First().Param3}");
        //    //    if (s.Key == "ABANDON")
        //    //        Console.WriteLine($"Position {s.First().Param1} Einstiegsposition {s.First().Param2} Wartezeit {s.First().Param3}");
        //    //}
        //}
        //data.ForEach(dataLine => {Console.WriteLine(dataLine);});
        //PlotData(data);
        CreateSampleChartImage("Image.bmp");
        Run("Image.bmp");
    }

    private static ProgramOptions ProcessProgramArgs(string[] args)
    {
        var pOptions = new ProgramOptions();
        return pOptions;

    }

    public static void DisplayPromt() { Console.WriteLine(); }
    public static void Run(string command, /*out string output, out string error,*/ string? directory = null)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                Arguments = "/c " + command,
                CreateNoWindow = true,
                WorkingDirectory = directory ?? string.Empty,
            }
        };
        process.Start();
        //process.WaitForExit();
        //output = process.StandardOutput.ReadToEnd();
        //error = process.StandardError.ReadToEnd();
    }
    public static string GetInfo(LogEntry? entry)
    {
        if (entry == null)
            return string.Empty;
        var wz = (entry.Event == "COMPLETECALLER" || entry.Event == "COMPLETEAGENT")
            ? $"Agent {entry.Channel} Wartezeit {entry.Param1} Anrufdauer {entry.Param2}"
            : entry.Event == "ABANDON"
            //? $"Wartezeit {entry.Param3}" 
            //: entry.Event == "CONNECT" 
            ? $"Wartezeit {entry.Param1}"
            : string.Empty;
        return $"{entry} {wz}";
    }

#pragma warning disable CA1416
    // The code that's violating the rule is on this line.
    static void CreateSampleChartImage(string imagePath)
    {
        const int width = 1500;
        const int height = 1000;
        const int margin = 100;
        using var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        // Draw the axes
        g.DrawLine(Pens.Black, margin, height - margin, margin, margin); // Y-axis
        g.DrawLine(Pens.Black, margin, height - margin, width - margin, height - margin); // X-axis

        // Example data for the chart
        string[] categories = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        int[] values = { 10, 20, 30, 40, 30, 50, 20, 40, 10, 30, 20, 50 };
        string[] ycategories = { "0", "1", "2", "3" };
        using var current = new System.Drawing.Font("Arial", 8);
        for (var i = 0; i < categories.Length; i++)
        {
            var x = margin + 20 + i * 80; // Position of the bar
            var y = height - margin - values[i] * 4; // Height of the bar
            Console.WriteLine($"{x}, {y}");
            g.FillRectangle(Brushes.Blue, x, y, 60, values[i] * 4); // Draw the bar
            g.DrawString(categories[i], current, Brushes.Black, x, height - 40); // Label
        }

        for (var i = 0; i < ycategories.Length; i++)
        {
            var x = margin-30;
            var y = height - margin-20 - (i * (height - 2 * margin) / (ycategories.Length - 1));
            Console.WriteLine($"{x}, {y}");
            g.DrawString(ycategories[i], current, Brushes.Black, x, y);
        }
        File.Delete(imagePath);
        // Save the chart as PNG
        bmp.Save(imagePath);
    }
#pragma warning restore CA1416
    private static void PlotData(List<object> data)
    {
        //// Plot erstellen
        //var plt = new ScottPlot.Plot(600, 400);

        //// Daten plotten
        //foreach (var call in data)
        //{
        //    var color = call.Status == "ANSWERED" ? System.Drawing.Color.Green : System.Drawing.Color.Red;
        //    plt.AddBar(call.Timestamp.ToOADate(), call.Duration / 3600.0, color); // Dauer in Stunden umrechnen
        //}

        //// Achsen beschriften und formatieren
        //plt.XAxis.Label("Zeit");
        //plt.YAxis.Label("Mitarbeiter");
        //plt.XAxis.DateTimeFormat(true);

        //// Plot speichern oder anzeigen
        //plt.SaveFig("anruf_plot.png"); // Speichern als PNG
        //plt.Show(); // Anzeigen
    }
}

