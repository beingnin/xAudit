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
        /// <summary>
        /// Not yet implemented
        /// </summary>
        /// <returns></returns>
        public Setup UseTriggers()
        {
            _type = Implementation.Triggers;
            return this;
        }
        /// <summary>
        /// Configure replicator in a way that it wouldn't mind the schema changes happened on tables. Will continue using the previous table structure only
        /// </summary>
        /// <returns></returns>
        public Setup DoNotTrackSchemaChanges()
        {
            _trackSchemaChanges = false;
            return this;
        }
        /// <summary>
        /// Replicator will try to make adjustments to those fields which are not convertible to target data type when a data type change occurs in a column. Might result into data loss
        /// </summary>
        /// <returns></returns>
        public Setup AllowDataLoss()
        {
            _forceMerge = true;
            return this;
        }
        /// <summary>
        /// Note: excluding a previously added table from this list will stop replicating data for that table
        /// </summary>
        /// <param name="tables">Pass the audit table collection which you need to replicate data</param>
        /// <returns></returns>
        public Setup Tables(AuditTableCollection tables)
        {
            _tables = tables;
            return this;
        }
        /// <summary>
        /// Instance name of this replicator. This will also be the name of the database schema where xAudit creats for it's db operations. Use this to change it if you are alreading using the default instance name in the db 
        /// </summary>
        /// <param name="instance">Default is xAudit</param>
        /// <returns></returns>
        public Setup SetInstanceName(string instance)
        {
            _instance = instance;
            return this;
        }
        /// <summary>
        /// Folder path where all replicated data(.mdf) will be kept if it is other than the PRIMARY file group location. This can be a location in the same disk drive or a new one or even in a separate machine in the same network. Set it wisely according to your environment structure and requirements. Reccommendation is to use a separate disk drive in the same machine considering benchmarks done wrt perfomance and latency
        /// </summary>
        /// <param name="directoryPath">setting null will force the tool to use the same location of PRIMARY file group</param>
        /// <returns></returns>
        public Setup Directory(string directoryPath)
        {
            _directory = directoryPath.TrimEnd('\\');
            return this;
        }
        /// <summary>
        /// Build and return the replicator object in which you can start replications
        /// </summary>
        /// <returns>IReplicator object</returns>
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
