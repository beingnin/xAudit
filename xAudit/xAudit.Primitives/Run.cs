using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Shared
{
    public class Run
    {
        public int Id { get; set; }
        public bool TrackSchemaChanges { get; set; }
        public bool ForceMerge { get; set; }
        public string InstanceName { get; set; }
        public string MachineName { get => Environment.MachineName; }
        public List<RunTable> RunTables { get; set; }
        public List<Log> Logs { get; set; }
        public DateTime RunAt { get; set; }

    }
}
