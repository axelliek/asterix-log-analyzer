using System.Data;
using System.Drawing;
using System.Diagnostics;
using Asterix_Log_Analyzer.Domain;
using Asterix_Log_Analyzer.Logging;
using Asterix_Log_Analyzer.Chart;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Markup;

namespace Asterix_Log_Analyzer;

class Program
{
    private const char LineSeparator = '|';

    static void Main(string[] args)
    {

        var programOptions = ProcessProgramArgs(args);

        try
        {
            List<LogEntry>? data = GetAllLogEntries(programOptions.InputFilePath);

            List<CallInfo> calls = ConvertLogsToCalls(data, out long? firstCall, out long? lastCall);

#if DEBUG
            WriteCallsInformation(calls);
#endif
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
        var directory = Directory.CreateDirectory(programOptions.OutputDirectory);
        var inputFileName = Path.GetFileNameWithoutExtension(programOptions.InputFilePath);
        var fileName = $"{inputFileName}-{DateTime.Now:yyMMdd-HHmmss}.bmp";

        //Console.WriteLine(fileName);
        var imageFullName = Path.Combine(directory.FullName, fileName);
        //Console.WriteLine(imageFullName);

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


        Debug.WriteLine($"First Call\t{d[0]?.FirstOrDefault()?.CallID}\t{d[0]?.FirstOrDefault()?.Timestamp}");
        Debug.WriteLine($"Last Call\t{d[^1]?.FirstOrDefault()?.CallID}\t{d[^1]?.FirstOrDefault()?.Timestamp}");


        var duration = TimeSpan.FromSeconds((double)(lastCall - firstCall));
        var startTime = firstCall;

        Console.WriteLine($"Duration : {duration}");

        var calls = new List<CallInfo>();
        d.ForEach(item => { calls.Add(GetCallInfo(item)); });

        return calls;
    }

    private static void WriteCallsInformation(List<CallInfo> calls)
    {
        var callsAbandoned = calls.Where(x => x.CallStatus == CallInfo.StatusAbandon).Count();
        var callsCompleted = calls.Where(x => x.CallStatus == CallInfo.StatusComplete).Count();
        var callsConnect = calls.Where(x => x.CallStatus == CallInfo.StatusConnect).Count();
        var callsWaited = calls.Where(x => x.CallStatus == CallInfo.StatusWaited).Count();

        Console.WriteLine($"Abgeschlossene Anrufe {callsCompleted} ");
        Console.WriteLine($"Abgebrochene Anrufe {callsAbandoned} ");
        Console.WriteLine($"Angenommene Anrufe {callsConnect} ");
        Console.WriteLine($"Wartende Anrufe {callsWaited} ");
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
        {
            ArgumentNullException argumentNullException = new($"{nameof(GenerateChartInfo)}: Parameter {nameof(startTime)} or {nameof(endTime)} are null or empty");
            throw argumentNullException;
        }

        ChartInfo chartInfo = new()
        {
            TimeStart = DateTimeOffset.FromUnixTimeSeconds((long)startTime!).DateTime.ToString("HH:MM:ss"),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds((long)endTime!).DateTime.ToString("HH:MM:ss"),
            StartTime = (long)startTime!,
            EndTime = (long)endTime!,
        };

        List<string> xAxisLabels = []; // new List<string>();

        DateTimeOffset dt = DateTimeOffset.FromUnixTimeSeconds((long)startTime);
        for (long i = (long)startTime; i < endTime; i += 600) //15min * 60 secs 
        {
            dt = dt.AddSeconds(900);//.DateTime;
            Console.WriteLine(dt);
            xAxisLabels.Add($"{dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}");
        }

        chartInfo.XCategories = xAxisLabels;
        chartInfo.YCategories = [];

        List<List<StackedBarValues>> values = [new List<StackedBarValues>()];

        bool InitSlot(int slotIndex, StackedBarValues t)
        {
            if (slotIndex > values.Count - 1)
            {
                Debug.WriteLine($"Current slot {slotIndex}");
                values.Add(new List<StackedBarValues>());
                values[slotIndex].Add(t);
                return true;
            }
            Debug.WriteLine($"Current slot {slotIndex} {values[slotIndex].Count}");
            return false;
        }
        int currentslot = 0;


        void AddToSlots(StackedBarValues t)
        {
            if (InitSlot(currentslot, t)) return;

            //if (values[currentslot].Count == 0) { values[currentslot].Add(t); return; }
            //for (var j = 0; j < values[currentslot].Count; j++)
            foreach (var list in values)
            {
                if (InitSlot(currentslot, t)) return;
                if (SlotIsBusy(list, t))
                {
                    currentslot++;
                    continue;
                }

                if (!SlotIsBusy(list, t))
                {
                    list.Add(t);
                    currentslot = 0;
                    return;
                }

            }
            //currentslot++;
            InitSlot(currentslot, t);
            currentslot = 0;

        }

        foreach (var call in calls)
        {
            var t = new StackedBarValues { Start = call.CallStart, End = call.CallEnd, Wait = call.CallWaittime, Speak = call.CallSpeaktime, Status = call.CallStatus };

            //var call = calls[k];
            AddToSlots(t);

        }
        for(int title = 0; title < values.Count; title++)
        {
            chartInfo.YCategories.Add($"{title+1}");
        }
        chartInfo.Values = values;
        return chartInfo;
    }
    static bool Intersect(StackedBarValues call, StackedBarValues current)
    {
        if (current.Start >= call.End) return false;


        return true;
    }
    private static bool SlotIsBusy(List<StackedBarValues> stackedBarValues, StackedBarValues current)
    {
        foreach (var call in stackedBarValues)
        {
            bool isBusy = Intersect(call, current);
            if (isBusy) return true;
        }
        return false;
    }

    private static CallInfo GetCallInfo(IGrouping<string, LogEntry> item)
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
            OutputDirectory = @".\Output"
        };

        if (args.Length == 1)
        {
            pOptions.InputFilePath = args[0];
        }

        return pOptions;

    }

    private static void DisplayPrompt()
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
    }

#pragma warning disable CA1416
    // The code that's violating the rule is on this line.
    private static bool CreateChartBitmap(string imagePath, ChartInfo chartInfo)
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

        var barHight = (height - 2 * margin) / (yCategories.Length );


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
            var yText = height - margin - ((i+1) * barTextYMargin);

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
