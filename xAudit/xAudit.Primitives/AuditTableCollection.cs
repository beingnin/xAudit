using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xAudit.Primitives
{
    public class AuditTableCollection: Dictionary<string, string[]>
    {
        public new int Count { 
            get
            {
                if (this.Values == null)
                    return default(int);
                return this.Values.Sum(x => x.Length);
            }
        }

    }
}
