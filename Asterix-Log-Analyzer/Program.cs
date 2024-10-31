using System.Diagnostics;
using Asterix_Log_Analyzer.Domain;
using Asterix_Log_Analyzer.Logging;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Markup;

namespace Asterix_Log_Analyzer;

partial class Program
{
   

    static void Main(string[] args)
    {

        ProgramOptions.ProcessProgramArgs(args);

        try
        {
            // Load log file, arrange and convert to log entries fo future use
            List<LogEntry>? logEntries = LogEntryReader.GetAllLogEntries(ProgramOptions.InputFilePath);

            //TODO:
            List<CallInfo> calls = LogsToCallsConverter.ConvertLogsToCalls(logEntries, out long? firstCall, out long? lastCall);

            string imageFullName = ProgramOptions.GetBitmapFileName();

            var chartInfo = ChartInfoConverter.GenerateChartInfo(calls, firstCall, lastCall);

            if (Chart.CreateChartBitmap(imageFullName, chartInfo))
            {
                RunProcess(imageFullName);
            }
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
