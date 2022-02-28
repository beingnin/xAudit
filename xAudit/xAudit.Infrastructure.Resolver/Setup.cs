using System;
using xAudit.CDC;

namespace xAudit
{
    public class Setup
    {
        private enum Implementation
        {
            CDC, Triggers
        }
        private Implementation _type;
        private string _sourceConnection = null;
        private string _instance = null;
        private string _directory = null;
        private string _partitionConnection = null;
        private bool _trackSchemaChanges = true;
        private bool _forceMerge = false;
        private AuditTableCollection _tables = null;
        public Setup(string sourceConnection)
        {
            _sourceConnection = sourceConnection;
            _partitionConnection = sourceConnection;
        }
        /// <summary>
        /// Calling this method will set the internal implementation dependency to capture change data 
        /// </summary>
        /// <returns></returns>
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

        public Setup DoNotTrackSchemaChanges()
        {
            _trackSchemaChanges = false;
            return this;
        }
        public Setup AllowDataLoss()
        {
            _forceMerge = true;
            return this;
        }
        public Setup Tables(AuditTableCollection tables)
        {
            _tables = tables;
            return this;
        }
        public Setup SetInstanceName(string instance)
        {
            _instance = instance;
            return this;
        }
        public Setup Directory(string directoryPath)
        {
            _directory = directoryPath.TrimEnd('\\');
            return this;
        }
        public IReplicator GetReplicator()
        {
            switch (this._type)
            {
                case Implementation.CDC:
                    return ReplicatorUsingCDC.GetInstance(
                        new CDCReplicatorOptions()
                        {
                            TrackSchemaChanges = _trackSchemaChanges,
                            InstanceName = _instance,
                            DataDirectory = _directory,
                            ForceMerge = _forceMerge,
                            Tables = _tables
                        }
                        , _sourceConnection
                        , _partitionConnection);
                case Implementation.Triggers:
                    throw new NotImplementedException("Replicator using Triggers have not yet implemented. Please use CDC as the processor");
                default:
                    return null;
            }
        }
    }
}
