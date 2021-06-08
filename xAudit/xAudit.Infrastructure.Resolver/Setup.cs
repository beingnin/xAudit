﻿using System;
using xAudit.Contracts;
using xAudit.CDC;
using System.Collections.Generic;

namespace xAudit.Infrastructure.Resolver
{
    public class Setup
    {
        private enum Implementation
        {
            CDC, Triggers
        }
        private Implementation _type;
        private string _sourceConnection = null;
        private string _partitionConnection = null;
        private bool _replicateIfRecreating = false;
        private bool _recreateIfSchemaChanged = true;
        private IDictionary<string, string> _tables = null;
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
       
        public Setup ReplicateBeforeRecreation()
        {
            _replicateIfRecreating = true;
            return this;
        }
        public Setup DoNotReplicateOnSchemaChanges()
        {
            _recreateIfSchemaChanged = false;
            return this;
        }
        public Setup Tables(Dictionary<string, string> tables)
        {
            _tables = tables;
            return this;
        }
        public IReplicator GetReplicator()
        {
            switch (this._type)
            {
                case Implementation.CDC:
                    return ReplicatorUsingCDC.GetInstance(
                        new CDCReplicatorOptions(){ 
                            ReplicateIfRecreating = _replicateIfRecreating,
                            RecreateIfSchemaChanged = _recreateIfSchemaChanged, 
                            Tables = _tables }
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
