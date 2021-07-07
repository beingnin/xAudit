using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace xAudit
{
    public interface IReplicator
    {
        Version CurrentVersion { get; }
        Task StartAsync();
        void Start();
        Task PartitionAsync(string schema,string table, string version);
        void Partition(string schema, string table, string version);
        Task StopAsync(bool cleanup);
        void Stop(bool backupBeforeDisabling=true, bool cleanSource = true);
    }
}
