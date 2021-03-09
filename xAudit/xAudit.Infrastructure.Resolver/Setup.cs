using System;
using xAudit.Contracts;
using xAudit.CDC;
namespace xAudit.Infrastructure.Resolver
{
    public class Setup
    {
        private enum Implementation
        {
            CDC, Triggers
        }
        private Implementation _type;
        private string _connectionString = null;
        private bool _alwaysRecreateTables = false;
        private bool _replicateIfRecreating = false;
        private bool _recplicateIfSchemaChanged = false;
        public Setup(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Setup UseCDC()
        {
            _type = Implementation.CDC;
            return this;
        }
        public Setup UseTriggers()
        {
            _type = Implementation.Triggers;
            return this;
        }
        public Setup RecreateOnStart()
        {
            _alwaysRecreateTables = true;
            return this;
        }
        public Setup ReplicateBeforeRecreation()
        {
            _replicateIfRecreating = true;
            return this;
        }
        public Setup ReplicateOnSchemaChanges()
        {
            _recplicateIfSchemaChanged = true;
            return this;
        }
        public IReplicator GetReplicator()
        {
            switch (this._type)
            {
                case Implementation.CDC:
                    return  ReplicatorUsingCDC.GetInstance(_connectionString);
                case Implementation.Triggers:
                   throw new NotImplementedException("Replicator using Triggers have not yet implemented. Please use CDC as the processor");
                default:
                    return null;
            }
        }
    }
}
