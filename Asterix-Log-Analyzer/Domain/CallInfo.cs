using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asterix_Log_Analyzer.Domain
{
    public class CallInfo
    {
        public const string StatusComplete = "COMPLETE";
        public const string StatusAbandon = "ABANDON";
        public const string StatusConnect = "CONNECT";
        public const string StatusWaited = "WAITED";
        public const string StatusUnknown = "UNKNOWN";

        public long CallStart { get; set; } = 0L; //=> this.CallEnd - this.CallWaittime - this.CallSpeaktime;//{ get; set; } = 0L;
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
