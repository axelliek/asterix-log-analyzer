using AsterixLogAnalyzer.Chart;
using System.Diagnostics;

namespace AsterixLogAnalyzer.Domain;

/**
 * <summary>Represents chart information converter</summary>
 */
public class ChartInfoConverter
{

    private const int DefaultTimeBarTick = 5 * 60; // 5 minutes * 60 seconds

    /**
     * <summary>TimeBarTick in seconds</summary>
     */
    public static int TimeBarTick { get; set; } = DefaultTimeBarTick;

    /**
     * <summary>Arrange time bar ticks property</summary>
     */
    public static bool ArrangeTimeTicks { get; set; } = true;

    /**
     * <summary>Generates chart information structure</summary>
     */
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
        };

        if (ArrangeTimeTicks)
        {
            ArrangeTimeBarTicks(TimeBarTick, ref startTime, ref endTime);
        }


        List<string> xAxisLabels = [];
        for (long i = (long)startTime!; i <= endTime; i += TimeBarTick)
        {
            DateTimeOffset dt = DateTimeOffset.FromUnixTimeSeconds((long)i);
            xAxisLabels.Add($"{dt.Hour:D2}:{dt.Minute:D2}"); //:{ dt.Second:D2} 
        }



        chartInfo.XCategories = xAxisLabels;
        chartInfo.YCategories = [];
        List<List<StackedBarValues>> values = CalculateSlots(calls);

        for (int title = 0; title < values.Count; title++)
        {
            chartInfo.YCategories.Add($"{title + 1}");
        }

        chartInfo.StartTime = (long)startTime!;
        chartInfo.EndTime = (long)endTime!;

        chartInfo.Values = values;

        return chartInfo;
    }

    /**
     * <summary>Arrange time bar to be at zero seconds</summary>
     */
    private static void ArrangeTimeBarTicks(int timeBarTick, ref long? startTime, ref long? endTime)
    {
        DateTimeOffset dt1 = DateTimeOffset.FromUnixTimeSeconds((long)startTime!);
        var seconds = dt1.Second;
        var diff = (endTime - (startTime - seconds)) % timeBarTick;
        startTime -= seconds;
        endTime += diff;
    }

    /**
     * <summary>Calculates slots</summary>
     */
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

    /**
     * <summary>CAlculates slot is busy</summary>
     */
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

