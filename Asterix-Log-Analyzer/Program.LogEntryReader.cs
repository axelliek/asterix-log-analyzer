using System.Data;
using Asterix_Log_Analyzer.Logging;

namespace Asterix_Log_Analyzer;

partial class Program
{
    public class LogEntryReader
    {
        public static List<LogEntry>? GetAllLogEntries(string inputFilePath)
        {
            List<LogEntry>? data;
            var text = File.ReadAllLines(inputFilePath);
            data = text
                .Select(line => line.Split(LineSeparator))
                .Select(parts => new LogEntry()
                {
                    Timestamp = parts[0],
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
    }
}
