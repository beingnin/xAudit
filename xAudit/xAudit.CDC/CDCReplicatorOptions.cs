using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC
{
    public class CDCReplicatorOptions
    {
        public bool TrackSchemaChanges { get; set; }
        public bool EnablePartition { get; set; }
        public string DataDirectory { get; set; }
        public bool KeepVersionsForPartition { get; set; }
        private string _instance;
        public string InstanceName
        {
            get => _instance ?? "xAudit";
            set => _instance = value;
        }
        public AuditTableCollection Tables { get; set; }
       
    }
}
