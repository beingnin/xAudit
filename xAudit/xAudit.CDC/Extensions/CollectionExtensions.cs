using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Extensions
{
    public static class CollectionExtensions
    {
        public static AuditTableCollection ToAuditCollection(this IEnumerable<KeyValuePair<string, string[]>> keyValuePairs)
        {
            if (keyValuePairs == null)
                return null;

            AuditTableCollection result = new AuditTableCollection();
            foreach(var key in keyValuePairs)
            {
                result.Add(key.ToString(), key.Value);
            }
            return result;
        }
    }
}
