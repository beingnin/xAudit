using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC
{
    public class CDCReplicatorOptions
    {
        public bool TrackSchemaChanges { get; set; }
        public bool EnablePartition { get; set; }
        public bool KeepVersionsForPartition { get; set; }
        public IDictionary<string, string> Tables { get; set; }
       
    }
}
