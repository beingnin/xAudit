using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC
{
    public class CDCReplicatorOptions
    {
        public bool PartitionIfSchemaChange { get; set; }
        public bool MergeIfSchemaChange { get; set; }
        public IDictionary<string, string> Tables { get; set; }
       
    }
}
