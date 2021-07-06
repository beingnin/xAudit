using System.Collections.Generic;
using System.Linq;
using xAudit.CDC.Extensions;

namespace xAudit.CDC.Helpers
{
    public static class AuditTableCollectionHelper
    {
        public static AuditTableCollection FromInstances(HashSet<string> instances)
        {
            if (instances == null || instances.Count() == 0)
                return null;

            return (AuditTableCollection)instances.Select(x => x.SplitInstance())
                                  .GroupBy(x => x.Item1)
                                  .Select(x => new KeyValuePair<string, string[]>(x.Key, x.Select(y => y.Item2).ToArray()))
                                  .ToDictionary(x=>x.Key,x=>x.Value);
        }
    }
}
