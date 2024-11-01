using System.Data;
using System.Diagnostics;

namespace AsterixLogAnalyzer.Domain;

/**
 * <summary>class LogsToCallsConverter</summary>
 */
public class LogsToCallsConverter
{
    /**
     * <summary>Converts LogEntries to calls</summary>
     */
    public static List<CallInfo> ConvertLogsToCalls(List<LogEntry>? data, out long? firstCall, out long? lastCall)
    {

        // Eingehende Anrufe
        var cleanedData = data!.Where(l => l.CallID != "NONE").GroupBy(x => x.CallID).ToList();

        firstCall = long.Parse(cleanedData[0]?.FirstOrDefault()?.Timestamp!);
        lastCall = long.Parse(cleanedData[^1]?.FirstOrDefault()?.Timestamp!);

        // Eingeloggte Agenten
        var agentsLoggedIn = data!.GroupBy(x => x.Channel).Count();

        Debug.WriteLine($"Eingeloggte Agenten {agentsLoggedIn}");
        Debug.WriteLine($"Anzahl Anrufe {cleanedData.Count}");

        string[] find = ["ABANDON", "COMPLETEAGENT", "AGENTDUMP", "COMPLETECALLER"];
        string[] entered = ["ENTERQUEUE"];
        string[] connected = ["CONNECT"];


        var duration = TimeSpan.FromSeconds((double)(lastCall - firstCall));
        var startTime = firstCall;

        Debug.WriteLine($"Log time : {duration}");

        var calls = new List<CallInfo>();
        cleanedData.ForEach(item => { calls.Add(GetCallInfo(item)); });
#if DEBUG
        WriteCallsInformation(calls);
#endif
        return calls;
    }

    private static void WriteCallsInformation(List<CallInfo> calls)
    {
        var callsAbandoned = calls.Where(x => x.CallStatus == CallInfo.StatusAbandon).Count();
        var callsCompleted = calls.Where(x => x.CallStatus == CallInfo.StatusComplete).Count();
        var callsConnect = calls.Where(x => x.CallStatus == CallInfo.StatusConnect).Count();
        var callsWaited = calls.Where(x => x.CallStatus == CallInfo.StatusWaited).Count();

        Console.WriteLine($"Completed calls {callsCompleted} ");
        Console.WriteLine($"Abandoned calls {callsAbandoned} ");
        Console.WriteLine($"Calls connected but not ended {callsConnect} ");
        Console.WriteLine($"Ringing calls but not ended {callsWaited} ");
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
}

