using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Shared
{
    public class Run
    {
        public int Id { get; set; }
        public bool Archive { get; set; }
        public bool TrackSchemaChanges { get; set; }
        public string InstanceName { get; set; }
        public string MachineName { get; set; }
        public List<RunTables> RunTables { get; set; }
        public DateTime MyProperty { get; set; }

    }
}
