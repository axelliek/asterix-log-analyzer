using AsterixLogAnalyzer.Chart;
using System.Diagnostics;

namespace AsterixLogAnalyzer.Domain;


public class ChartInfoConverter
{
    public static ChartInfo GenerateChartInfo(List<CallInfo> calls, long? startTime, long? endTime)
    {
        if (startTime == null || endTime == null)
        {
            var message = $"{nameof(GenerateChartInfo)}: Parameter {nameof(startTime)} or {nameof(endTime)} are null or empty";
            ArgumentNullException argumentNullException = new(message);
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

            xAxisLabels.Add($"{dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}");
        }

        chartInfo.XCategories = xAxisLabels;
        chartInfo.YCategories = [];
        List<List<StackedBarValues>> values = CalculateSlots(calls);

        for (int title = 0; title < values.Count; title++)
        {
            chartInfo.YCategories.Add($"{title + 1}");
        }
        chartInfo.Values = values;
        return chartInfo;
    }

    private static List<List<StackedBarValues>> CalculateSlots(List<CallInfo> calls)
    {
        List<List<StackedBarValues>> values = [[]];

        bool InitSlot(int slotIndex, StackedBarValues t)
        {
            if (slotIndex > values.Count - 1)
            {
                Debug.WriteLine($"Current slot {slotIndex}");
                values.Add([]);
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

            InitSlot(currentslot, t);
            currentslot = 0;
        }

        foreach (var call in calls)
        {
            StackedBarValues t = new()
            {
                Start = call.CallStart,
                End = call.CallEnd,
                Wait = call.CallWaittime,
                Speak = call.CallSpeaktime,
                Status = call.CallStatus
            };

            AddToSlots(t);

        }

        return values;
    }

    private static bool SlotIsBusy(List<StackedBarValues> stackedBarValues, StackedBarValues current)
    {
        foreach (var call in stackedBarValues)
        {
            bool isBusy = current.Intersect(call);
            if (isBusy) return true;
        }
        return false;
    }

}

