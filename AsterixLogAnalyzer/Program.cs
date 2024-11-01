using System.Diagnostics;
using AsterixLogAnalyzer.Domain;
using AsterixLogAnalyzer.Chart;

namespace AsterixLogAnalyzer;

partial class Program
{


    static void Main(string[] args)
    {

        if (!ProgramOptions.ProcessProgramArgs(args)) return;

        try
        {
            // Load log file, arrange and convert to log entries fo future use
            List<LogEntry>? logEntries = LogEntryReader.GetAllLogEntries(ProgramOptions.InputFilePath);

            List<CallInfo> calls = LogsToCallsConverter.ConvertLogsToCalls(logEntries, out long? firstCall, out long? lastCall);
            var chartInfo = ChartInfoConverter.GenerateChartInfo(calls, firstCall, lastCall);
            using var bitmap = new StackedBarChart(chartInfo).Bitmap;

            if (bitmap == null)
            {
                return;
            }

            string imageFullName = ProgramOptions.GetBitmapFileName();

#pragma warning disable CA1416
            bitmap.Save(imageFullName);
#pragma warning restore CA1416

            if (!File.Exists(imageFullName))
            {
                Console.WriteLine($"ERROR: Bitmap was not created.");
                return;
            }
            Console.WriteLine($"Bitmap is written to:\n{imageFullName}");

            RunProcess(imageFullName);
        }
        catch (ArgumentNullException aex)
        {
            Console.WriteLine(aex.Message);
        }
        catch (FileNotFoundException fex)
        {
            Console.WriteLine(fex.Message);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

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
}
