// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

//using ScottPlot;

using System.Data;
using System.Drawing;
using System.Diagnostics;

namespace Asterix_Log_Analyzer;
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
        return $"{CallID}\t{Event}";// base.ToString();
    }
}

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




internal class Agent
{

    public required string AgentId { get; set; }
}

public class CallInfo
{
    public const string StatusComplete = "COMPLETE";
    public const string StatusAbandon = "ABANDON";
    public const string StatusConnect = "CONNECT";
    public const string StatusWaited = "WAITED";
    public const string StatusUnknown = "UNKNOWN";

    public long CallStart { get; set; } = 0L; //=> this.CallEnd - this.CallWaittime - this.CallSpeaktime;//{ get; set; } = 0L;
    public long CallEnd { get; set; } = 0L;
    public string CallStatus { get; set; } = CallInfo.StatusUnknown;
    public long CallWaittime { get; set; } = 0L;
    public long CallSpeaktime { get; set; } = 0L;
    public string AgentId { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public override string ToString()
    {
        return $"{CallId},\t{CallStart},\t {CallEnd},\t {CallWaittime},\t {CallSpeaktime},\t {CallStatus}";
    }
}

/**
 * <summary>Represents Program start options</summary>
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

        //args.ToList().ForEach(arg => { Console.WriteLine(arg.ToString()); });
        var programOptions = ProcessProgramArgs(args);

        //string filePath = @".\Data\Testdaten.txt"; // Anpassung erforderlich, wenn die Datei woanders liegt
        List<LogEntry>? data = null;

        try
        {
            var text = File.ReadAllLines(programOptions.InputFilePath);
            data = text
                .Select(line => line.Split(LineSeparator))
                .Select(parts => new LogEntry()
                {
                    Timestamp = parts[0], //DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(parts[0])).DateTime, // FromFileTime() ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", null),
                    CallID = parts[1],
                    Queue = parts[2],
                    Channel = parts[3],
                    Event = parts[4],
                    Param1 = parts.Length >= 6 ? parts[5] : string.Empty,
                    Param2 = parts.Length >= 7 ? parts[6] : string.Empty,
                    Param3 = parts.Length >= 8 ? parts[7] : string.Empty,
                })
                .ToList();
            if (data == null || (data?.Count == 0)) throw new Exception("Data is null or empty");
        }
        catch (ArgumentNullException aex) { Console.WriteLine(aex.Message); return; }
        catch (FileNotFoundException fex)
        {
            Console.WriteLine(fex.Message);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return;
        }

        // Eingehende Anrufe
        var d = data!.Where(l => l.CallID != "NONE").GroupBy(x => x.CallID).ToList();
        // Eingeloggte Agenten
        var sst = data!.GroupBy(x => x.Channel).Count();

        Console.WriteLine($"Eingeloggte Agenten {sst}");
        Console.WriteLine($"Anzahl Anrufe {d.Count}");
        string[] find = ["ABANDON", /*"ENTERQUEUE", "CONNECT",*/ /*"RINGNOANSWER",*/ "COMPLETEAGENT", "AGENTDUMP", "COMPLETECALLER"/*, "ENTERQUEUE"*/];
        string[] entered = ["ENTERQUEUE"];
        string[] connected = ["CONNECT"];
        //Console.WriteLine($"Eingehende Anrufe {d.Count}");
        var firstCall = long.Parse(d[0]?.FirstOrDefault()?.Timestamp);
        var lastCall = long.Parse(d[^1]?.FirstOrDefault()?.Timestamp);
        Console.WriteLine($"First Call\t{d[0]?.FirstOrDefault()?.CallID}\t{d[0]?.FirstOrDefault()?.Timestamp}");
        Console.WriteLine($"Last Call\t{d[^1]?.FirstOrDefault()?.CallID}\t{d[^1]?.FirstOrDefault()?.Timestamp}");
        //var vv = d.GroupBy(x => x.Event).Where(x => find.Contains(x.Key) /* == "ABANDON" || x.Key == "CONNECT"*/ ).ToList(); //.ForEach(x => x);
        //vv.ForEach(t => Console.WriteLine(GetInfo(t.FirstOrDefault())));
        var duration = TimeSpan.FromSeconds(lastCall - firstCall);
        var startTime = firstCall;
        Console.WriteLine($"Duration : {duration}");
        //int count = 0;
        var calls = new List<CallInfo>();
        d.ForEach(item =>
        {
            var ii = GetCallInfo1(item);
            if (ii != null)
            {
                calls.Add(ii);
                Console.WriteLine(ii.ToString());
            }
            //var vv = item.GroupBy(x => x.Event).Where(x => find.Contains(x.Key)).ToList();
            //vv[0].FirstOrDefault().CallID
            //if (vv.Count == 1)
            //{
            //    count += vv.Count;
            //    Console.WriteLine(GetInfo(vv.Count == 1 ? vv[0]?.FirstOrDefault() : null));
            //    var call = GetCallInfo(vv[0]?.FirstOrDefault());
            //    Console.WriteLine(call.ToString());
            //    if (call != null)
            //        calls.Add(call);
            //}
        });

        var callsAbandoned = calls.Where(x => x.CallStatus == CallInfo.StatusAbandon).Count(); //.ToList().Count;
        var callsCompleted = calls.Where(x => x.CallStatus == CallInfo.StatusComplete).Count();
        var callsConnect = calls.Where(x => x.CallStatus == CallInfo.StatusConnect).Count();
        var callsWaited = calls.Where(x => x.CallStatus == CallInfo.StatusWaited).Count();

        Console.WriteLine($"Abgeschlossene Anrufe {callsCompleted} ");
        Console.WriteLine($"Abgebrochene Anrufe {callsAbandoned} ");
        Console.WriteLine($"Angenommene Anrufe {callsConnect} ");
        Console.WriteLine($"Wartende Anrufe {callsWaited} ");

        var directory = Directory.CreateDirectory(programOptions.OutputFilePath);
        var inputFileName = Path.GetFileNameWithoutExtension(programOptions.InputFilePath);
        var fileName = $"{inputFileName}-{DateTime.Now:yyMMdd-HHmmss}.bmp";
        Console.WriteLine(fileName);
        var imageFullName = Path.Combine(directory.FullName, fileName);
        Console.WriteLine(imageFullName);
        CreateSampleChartImage(imageFullName, GenerateChartInfo(calls, firstCall, lastCall));
        Run(imageFullName);
    }

    private static ChartInfo GenerateChartInfo(List<CallInfo> calls, long startTime, long endTime)
    {
        ChartInfo chartInfo = new ChartInfo
        {
            TimeStart = DateTimeOffset.FromUnixTimeSeconds(startTime).DateTime.ToString("HH:MM:ss"),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds(endTime).DateTime.ToString("HH:MM:ss")
        };
        var t = new List<string>
        {
            chartInfo.TimeStart
        };
        for (long i = startTime; i < endTime ; i += 900) //15*60
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(i);//.DateTime;
            
            t.Add($"{dt.AddMinutes(15).ToString("HH:MM:ss")}");
        }
        t.Add(chartInfo.TimeEnd);
        chartInfo.xCategories = t;
        chartInfo.yCategories = new List<string>(){ "1","2","3"};

        return chartInfo;
    }

    private static CallInfo GetCallInfo1(IGrouping<string, LogEntry> item)
    {
        string[] completing = ["COMPLETEAGENT", "COMPLETECALLER"];

        var enter = item.Where(x => "ENTERQUEUE".Contains(x.Event)).FirstOrDefault();
        var ringed = item.Where(x => "RINGNOANSWER".Contains(x.Event)).ToList();
        var conn = item.Where(x => "CONNECT".Contains(x.Event)).FirstOrDefault();
        var aband = item.Where(x => "ABANDON".Contains(x.Event)).FirstOrDefault();
        var compl = item.Where(x => completing.Contains(x.Event)).FirstOrDefault();

        CallInfo ci = new();

        if (enter != null)
        {
            ci.CallStart = long.Parse(enter.Timestamp!);
            ci.CallId = enter.CallID;
        }
        if (ringed.Count > 0)
        {
            var lastRing = long.Parse(ringed[^1].Timestamp!);
            ci.CallStatus = CallInfo.StatusWaited;
            ci.CallWaittime = lastRing - ci.CallStart;
            ci.CallEnd = lastRing;
        }
        if (conn != null)
        {
            ci.AgentId = conn.Channel!;
            ci.CallWaittime = long.Parse(conn.Param1!);
            ci.CallEnd = long.Parse(conn.Timestamp!);
            ci.CallStatus = CallInfo.StatusConnect;
        }

        if (compl != null)
        {
            ci.CallStatus = CallInfo.StatusComplete;
            ci.CallSpeaktime = long.Parse(compl.Param2!);
            ci.CallWaittime = long.Parse(compl.Param1!);
            ci.CallEnd = long.Parse(compl.Timestamp);
            return ci;
        }

        if (aband != null)
        {
            ci.CallStatus = CallInfo.StatusAbandon;
            ci.CallWaittime = long.Parse(aband.Param3!);
            ci.CallEnd = long.Parse(aband.Timestamp);
            return ci;
        }

        return ci;
    }



    private static ProgramOptions ProcessProgramArgs(string[] args, bool usetestdata = true)
    {
        if (args.Length == 0 || usetestdata)
            DisplayPrompt();

        var pOptions = new ProgramOptions()
        {
            InputFilePath = @".\Data\Testdaten.txt",
            OutputFilePath = @".\Output"
        };

        if (args.Length == 1)
        {
            pOptions.InputFilePath = args[0];
            pOptions.OutputFilePath = @".\Output";
        }
        return pOptions;

    }

    public static void DisplayPrompt()
    {
        Console.WriteLine("Prompt");
        Console.WriteLine("Asterix-Log-Analyzer.exe [<LOG_FILE> [-o <BITMAP_OUTPUT_DIRECTORY>]]");
    }

    public static void Run(string command, string? directory = null)
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
    public static CallInfo GetCallInfo(LogEntry? entry)
    {
        if (entry == null) return null;

        CallInfo ci = new CallInfo()
        {
            CallId = entry.CallID,
            AgentId = entry.Channel!,
            CallEnd = 0,
            CallSpeaktime = 0,
            CallStatus = CallInfo.StatusUnknown,
            CallWaittime = 0,
        };
        switch (entry.Event)
        {
            case "COMPLETECALLER":
            case "COMPLETEAGENT":
                ci.AgentId = entry.Channel!;
                ci.CallWaittime = long.Parse(entry.Param1!);
                ci.CallStatus = CallInfo.StatusComplete;
                ci.CallSpeaktime = long.Parse(entry.Param2!);
                ci.CallEnd = long.Parse(entry.Timestamp);
                //ci.CallStart = ci.CallEnd - ci.CallSpeaktime - ci.CallWaittime;
                break;
            case "ABANDON":
                ci.CallWaittime = long.Parse(entry.Param1!);
                ci.CallStatus = CallInfo.StatusAbandon;
                ci.CallEnd = long.Parse(entry.Timestamp!);
                //ci.CallStart = ci.CallEnd - ci.CallSpeaktime - ci.CallWaittime;
                break;


            default:
                break;

        }

        //var wz = (entry.Event == "COMPLETECALLER" || entry.Event == "COMPLETEAGENT")
        //    ? $"Agent {entry.Channel} Wartezeit {entry.Param1} Anrufdauer {entry.Param2}"
        //    : entry.Event == "ABANDON"
        //    //? $"Wartezeit {entry.Param3}" 
        //    //: entry.Event == "CONNECT" 
        //    ? $"Wartezeit {entry.Param1}"
        //    : string.Empty;
        //Console.WriteLine(ci.ToString());
        return ci;
    }

#pragma warning disable CA1416
    // The code that's violating the rule is on this line.
    static void CreateSampleChartImage(string imagePath, ChartInfo chartInfo)
    {
        int width = chartInfo.Width;
        int height = chartInfo.Height;
        int margin = chartInfo.Margin;

        using var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        // Draw the axes
        g.DrawLine(Pens.Black, margin, height - margin, margin, margin); // Y-axis
        g.DrawLine(Pens.Black, margin, height - margin, width - margin, height - margin); // X-axis

        // Example data for the chart
        string[] xCategories = chartInfo.xCategories!.ToArray();
        string[] categories = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        int[] values = { 10, 20, 30, 40, 30, 50, 20, 40, 10, 30, 20, 50 };
        string[] ycategories = { "0", "1", "2", "3" };
        string[] yCategories = chartInfo.yCategories!.ToArray();
        using var current = new System.Drawing.Font("Arial", 8);
        for (var i = 0; i < xCategories.Length; i++)
        {
            var x = margin + 20 + i * 80; // Position of the bar
            var y = height - margin - values[i] * 4; // Height of the bar
            Console.WriteLine($"{x}, {y}");
            g.FillRectangle(Brushes.Blue, x, y, 60, values[i] * 4); // Draw the bar
            g.DrawString(xCategories[i], current, Brushes.Black, x, height - 40); // Label
        }

        for (var i = 0; i < yCategories.Length; i++)
        {
            var x = margin - 30;
            var y = height - margin - 20 - (i * (height - 2 * margin) / (ycategories.Length - 1));
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

