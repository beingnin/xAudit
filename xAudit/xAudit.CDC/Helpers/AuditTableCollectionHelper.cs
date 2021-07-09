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

            return instances.Select(x => x.SplitInstance())
                            .GroupBy(x => x.Item1)
                            .Select(x => new KeyValuePair<string, string[]>(x.Key, x.Select(y => y.Item2).ToArray()))
                            .ToAuditCollection();
        }

        public static HashSet<string> ToHashSet(AuditTableCollection tables)
        {
            if (tables == null)
                return null;

            HashSet<string> result = new HashSet<string>();
            foreach (var schema in tables)
            {
                foreach (var table in schema.Value)
                {
                    result.Add(schema.Key + "." + table);
                }
            }
            return result;
        }
    }
}
