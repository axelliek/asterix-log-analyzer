namespace AsterixLogAnalyzer.Domain
{
    public class CallInfo
    {
        //TODO: move to enum

        public const string StatusComplete = "COMPLETE";
        public const string StatusAbandon = "ABANDON";
        public const string StatusConnect = "CONNECT";
        public const string StatusWaited = "WAITED";
        public const string StatusUnknown = "UNKNOWN";

        public long CallStart { get; set; } = 0L;
        public long CallEnd { get; set; } = 0L;
        public string CallStatus { get; set; } = StatusUnknown;
        public long CallWaittime { get; set; } = 0L;
        public long CallSpeaktime { get; set; } = 0L;
        public string AgentId { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"{CallId},\t{CallStart},\t {CallEnd},\t {CallWaittime},\t {CallSpeaktime},\t {CallStatus}";
        }
    }
}
