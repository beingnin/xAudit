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
        private bool _trackSchemaChanges = false;
        private bool _enablePartition = true;
        private bool _keepVersionsForPartitions = false;
        private AuditTableCollection _tables = null;
        public Setup(string sourceConnection, string partitionConnection = null)
        {
            _sourceConnection = sourceConnection;
            if (string.IsNullOrWhiteSpace(partitionConnection))
                _partitionConnection = sourceConnection;
            else
                _partitionConnection = partitionConnection;
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

        public Setup TrackSchemaChanges()
        {
            _trackSchemaChanges = true;
            return this;
        }
        public Setup EnablePartition(bool keepVersions = false)
        {
            _enablePartition = true;
            _keepVersionsForPartitions = keepVersions;
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
                            EnablePartition = _enablePartition,
                            KeepVersionsForPartition = _keepVersionsForPartitions,
                            InstanceName = _instance,
                            DataDirectory=_directory,
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
