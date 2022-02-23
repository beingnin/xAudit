using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Shared
{
    public class Log
    {
        public int Id { get; set; }
        public Run Run { get; set; }
        public string Message { get; set; }
        public MessageType Type { get; set; }
        public string Exception { get; set; }
        public string StackTrace { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public enum MessageType
    {
        Error,
        Warning
    }
}
