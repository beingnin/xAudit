using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC
{
    public class CDCReplicatorOptions
    {
        public bool AlwaysRecreateTables { get; set; }
        public bool ReplicateIfRecreating { get; set; }
        public bool ReplicateIfSchemaChanged { get; set; }
        public IDictionary<string, string> Tables { get; set; }
       
    }
}
