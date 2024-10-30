using System.Data;
using System.Drawing;
using System.Diagnostics;
using Asterix_Log_Analyzer.Domain;
using Asterix_Log_Analyzer.Logs;
using Asterix_Log_Analyzer.Chart;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace Asterix_Log_Analyzer;

class Program
{
    private const char LineSeparator = '|';

    static void Main(string[] args)
    {

        //args.ToList().ForEach(arg => { Console.WriteLine(arg.ToString()); });
        var programOptions = ProcessProgramArgs(args);

        //string filePath = @".\Data\Testdaten.txt"; // Anpassung erforderlich, wenn die Datei woanders liegt


        try
        {
            List<LogEntry>? data = GetAllLogEntries(programOptions.InputFilePath);

            List<CallInfo> calls = ConvertLogsToCalls(data, out long? firstCall, out long? lastCall);

            string imageFullName = GetBitmapFileName(programOptions);

            if (CreateChartBitmap(imageFullName, GenerateChartInfo(calls, firstCall, lastCall)))
            {
                RunProcess(imageFullName);
            }
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

    }

    private static string GetBitmapFileName(ProgramOptions programOptions)
    {
        var directory = Directory.CreateDirectory(programOptions.OutputFilePath);
        var inputFileName = Path.GetFileNameWithoutExtension(programOptions.InputFilePath);
        var fileName = $"{inputFileName}-{DateTime.Now:yyMMdd-HHmmss}.bmp";
        Console.WriteLine(fileName);
        var imageFullName = Path.Combine(directory.FullName, fileName);
        Console.WriteLine(imageFullName);
        return imageFullName;
    }

    private static List<CallInfo> ConvertLogsToCalls(List<LogEntry>? data, out long? firstCall, out long? lastCall)
    {

        // Eingehende Anrufe
        var d = data!.Where(l => l.CallID != "NONE").GroupBy(x => x.CallID).ToList();

        firstCall = long.Parse(d[0]?.FirstOrDefault()?.Timestamp!);
        lastCall = long.Parse(d[^1]?.FirstOrDefault()?.Timestamp!);

        // Eingeloggte Agenten
        var sst = data!.GroupBy(x => x.Channel).Count();

        Debug.WriteLine($"Eingeloggte Agenten {sst}");
        Debug.WriteLine($"Anzahl Anrufe {d.Count}");

        string[] find = ["ABANDON", /*"ENTERQUEUE", "CONNECT",*/ /*"RINGNOANSWER",*/ "COMPLETEAGENT", "AGENTDUMP", "COMPLETECALLER"/*, "ENTERQUEUE"*/];
        string[] entered = ["ENTERQUEUE"];
        string[] connected = ["CONNECT"];
        //Console.WriteLine($"Eingehende Anrufe {d.Count}");

        Console.WriteLine($"First Call\t{d[0]?.FirstOrDefault()?.CallID}\t{d[0]?.FirstOrDefault()?.Timestamp}");
        Console.WriteLine($"Last Call\t{d[^1]?.FirstOrDefault()?.CallID}\t{d[^1]?.FirstOrDefault()?.Timestamp}");
        //var vv = d.GroupBy(x => x.Event).Where(x => find.Contains(x.Key) /* == "ABANDON" || x.Key == "CONNECT"*/ ).ToList(); //.ForEach(x => x);
        //vv.ForEach(t => Console.WriteLine(GetInfo(t.FirstOrDefault())));
        var duration = TimeSpan.FromSeconds((double)(lastCall - firstCall));
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
        });

        var callsAbandoned = calls.Where(x => x.CallStatus == CallInfo.StatusAbandon).Count(); //.ToList().Count;
        var callsCompleted = calls.Where(x => x.CallStatus == CallInfo.StatusComplete).Count();
        var callsConnect = calls.Where(x => x.CallStatus == CallInfo.StatusConnect).Count();
        var callsWaited = calls.Where(x => x.CallStatus == CallInfo.StatusWaited).Count();

        Console.WriteLine($"Abgeschlossene Anrufe {callsCompleted} ");
        Console.WriteLine($"Abgebrochene Anrufe {callsAbandoned} ");
        Console.WriteLine($"Angenommene Anrufe {callsConnect} ");
        Console.WriteLine($"Wartende Anrufe {callsWaited} ");
        return calls;
    }
    private static List<LogEntry>? GetAllLogEntries(string inputFilePath)
    {
        List<LogEntry>? data;
        var text = File.ReadAllLines(inputFilePath);
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
        return data;
    }

    private static ChartInfo GenerateChartInfo(List<CallInfo> calls, long? startTime, long? endTime)
    {
        if (startTime == null || endTime == null)
            throw new ArgumentNullException($"{nameof(GenerateChartInfo)}: Parameter {nameof(startTime)} or {nameof(endTime)} are null or empty");

        ChartInfo chartInfo = new ChartInfo
        {
            TimeStart = DateTimeOffset.FromUnixTimeSeconds((long)startTime!).DateTime.ToString("HH:MM:ss"),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds((long)endTime!).DateTime.ToString("HH:MM:ss")
        };

        List<string> xAxisLabels = []; // new List<string>();

        DateTimeOffset dt = DateTimeOffset.FromUnixTimeSeconds((long)startTime);
        for (long i = (long)startTime; i < endTime; i += 600) //15min * 60 secs 
        {
            dt = dt.AddSeconds(900);//.DateTime;
            Console.WriteLine(dt);
            xAxisLabels.Add($"{dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}");
        }

        chartInfo.xCategories = xAxisLabels;
        chartInfo.yCategories = ["1", "2", "3"];

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

    private static void RunProcess(string command, string? directory = null)
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

    //private static string GetInfo(LogEntry? entry)
    //{
    //    if (entry == null)
    //        return string.Empty;
    //    var wz = (entry.Event == "COMPLETECALLER" || entry.Event == "COMPLETEAGENT")
    //        ? $"Agent {entry.Channel} Wartezeit {entry.Param1} Anrufdauer {entry.Param2}"
    //        : entry.Event == "ABANDON"
    //        //? $"Wartezeit {entry.Param3}" 
    //        //: entry.Event == "CONNECT" 
    //        ? $"Wartezeit {entry.Param1}"
    //        : string.Empty;
    //    return $"{entry} {wz}";
    //}

    //private static CallInfo GetCallInfo(LogEntry? entry)
    //{
    //    if (entry == null) return null;

    //    CallInfo ci = new CallInfo()
    //    {
    //        CallId = entry.CallID,
    //        AgentId = entry.Channel!,
    //        CallEnd = 0,
    //        CallSpeaktime = 0,
    //        CallStatus = CallInfo.StatusUnknown,
    //        CallWaittime = 0,
    //    };
    //    switch (entry.Event)
    //    {
    //        case "COMPLETECALLER":
    //        case "COMPLETEAGENT":
    //            ci.AgentId = entry.Channel!;
    //            ci.CallWaittime = long.Parse(entry.Param1!);
    //            ci.CallStatus = CallInfo.StatusComplete;
    //            ci.CallSpeaktime = long.Parse(entry.Param2!);
    //            ci.CallEnd = long.Parse(entry.Timestamp);
    //            //ci.CallStart = ci.CallEnd - ci.CallSpeaktime - ci.CallWaittime;
    //            break;
    //        case "ABANDON":
    //            ci.CallWaittime = long.Parse(entry.Param1!);
    //            ci.CallStatus = CallInfo.StatusAbandon;
    //            ci.CallEnd = long.Parse(entry.Timestamp!);
    //            //ci.CallStart = ci.CallEnd - ci.CallSpeaktime - ci.CallWaittime;
    //            break;


    //        default:
    //            break;

    //    }

    //    //var wz = (entry.Event == "COMPLETECALLER" || entry.Event == "COMPLETEAGENT")
    //    //    ? $"Agent {entry.Channel} Wartezeit {entry.Param1} Anrufdauer {entry.Param2}"
    //    //    : entry.Event == "ABANDON"
    //    //    //? $"Wartezeit {entry.Param3}" 
    //    //    //: entry.Event == "CONNECT" 
    //    //    ? $"Wartezeit {entry.Param1}"
    //    //    : string.Empty;
    //    //Console.WriteLine(ci.ToString());
    //    return ci;
    //}

#pragma warning disable CA1416
    // The code that's violating the rule is on this line.
    private static bool CreateChartBitmap(string imagePath, ChartInfo chartInfo)
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
        //string[] categories = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        int[] values = { 10, 20, 30, 40, 30, 50, 20, 40, 10, 30, 20, 50 };
        string[] ycategories = { "0", "1", "2", "3", "4" };
        string[] yCategories = chartInfo.yCategories!.ToArray();
        using var current = new System.Drawing.Font("Arial", 8);
        var step = (width - margin) / xCategories.Length;
        for (var i = 0; i < xCategories.Length; i++)
        {
            var x = margin + step * i; // Position of the bar
            var y = height - margin - values[i] * 4; // Height of the bar
            Debug.WriteLine($"DrawString {xCategories[i]} pos: (x:{x}, y:{y})");
            //g.FillRectangle(Brushes.Blue, x, y, 60, values[i] * 4); // Draw the bar
            g.DrawString(xCategories[i], current, Brushes.Black, x, height - margin + 20); // Label
        }
        var barHight = (height - 2 * margin) / (ycategories.Length - 1);
        var xAxisXOffset = margin;
        var xAxisYOffset = height - margin;
        for (var i = 0; i <= 1; i++)
        {
            g.FillRectangle(Brushes.Green, xAxisXOffset, xAxisYOffset - barHight, 100, barHight); // Draw the bar
            g.FillRectangle(Brushes.Red, xAxisXOffset + 100, xAxisYOffset - barHight, 100, barHight); // Draw the bar
            g.FillRectangle(Brushes.Yellow, xAxisXOffset + 200, xAxisYOffset - barHight, 100, barHight); // Draw the bar
        }


        var barTextYMargin = barHight;
        var barXOffset = 30;
        var ySlots = barHight * ycategories.Length;
        // Draw y labels
        for (var i = 0; i < ycategories.Length; i++)
        {
            // y axis label text position
            var xText = margin - barXOffset;
            var yText = height - margin - (i * barTextYMargin);

            Debug.WriteLine($"DrawString {ycategories[i]} pos: ( x:{xText}, y:{yText} )");

            g.DrawString(ycategories[i], current, Brushes.Black, xText, yText);
        }
        File.Delete(imagePath);
        // Save the chart as PNG
        bmp.Save(imagePath);
        return true;
    }
#pragma warning restore CA1416

}