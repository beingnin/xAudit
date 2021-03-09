using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using xAudit.Contracts;

namespace xAudit.CDC
{
    public class ReplicatorUsingCDC : IReplicator
    {
        private string _connectionString=null;
        private static Lazy<ReplicatorUsingCDC> _instance = new Lazy<ReplicatorUsingCDC>(()=>new ReplicatorUsingCDC());
        private ReplicatorUsingCDC()
        {
            Console.WriteLine("object created");
        }
        public static ReplicatorUsingCDC GetInstance(string connectionString)
        {
             _instance.Value._connectionString = connectionString;
            return _instance.Value;
        }
        public void Partition(string schema, string table, string version)
        {
            throw new NotImplementedException();
        }

        public Task PartitionAsync(string schema, string table, string version)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public void Stop(bool backupBeforeDisabling = true, bool cleanSource = true)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(bool cleanup)
        {
            throw new NotImplementedException();
        }
    }
}
