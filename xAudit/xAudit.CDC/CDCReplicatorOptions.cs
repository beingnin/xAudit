using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC
{
    public class CDCReplicatorOptions
    {
        public bool ReplicateIfRecreating { get; set; }
        public bool RecreateIfSchemaChanged { get; set; }
        public IDictionary<string, string> Tables { get; set; }
       
    }
}
