﻿using System.Data;

namespace AsterixLogAnalyzer.Domain;


/**
 * <summary>class LogEntryReader</summary>
 */
public class LogEntryReader
{
    private const char LineSeparator = '|';

    /**
     * <summary>Reads all log entries from file </summary>
     */
    public static List<LogEntry>? GetAllLogEntries(string filePath)
    {
        var allTextLinesInFile = File.ReadAllLines(filePath);

        var logEntries = allTextLinesInFile
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

        if (logEntries == null || (logEntries?.Count == 0))
        {
            throw new Exception("Data is null or empty");
        }

        return logEntries;
    }
}
