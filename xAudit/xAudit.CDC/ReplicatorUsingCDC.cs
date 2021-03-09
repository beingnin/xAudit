using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using xAudit.Contracts;

namespace xAudit.CDC
{
    public class ReplicatorUsingCDC : IReplicator
    {
        private string _sourceCon = null;
        private string _partitionCon = null;
        private IDictionary<string, string> _tables = null;
        private static Lazy<ReplicatorUsingCDC> _instance = new Lazy<ReplicatorUsingCDC>(() => new ReplicatorUsingCDC());
        private ReplicatorUsingCDC()
        {
            Console.WriteLine("object created");
        }
        public static ReplicatorUsingCDC GetInstance(IDictionary<string, string> tables, string sourceCon, string partitionCon)
        {
            _instance.Value._sourceCon = sourceCon;
            _instance.Value._tables = tables;
            _instance.Value._partitionCon = partitionCon;

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


        #region private-methods

        private Task ExecuteInitialScripts()
        {
            return null;
        }
                            
        #endregion
    }
}
