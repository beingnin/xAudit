using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using xAudit.Contracts;

namespace xAudit.CDC
{
    public class ArchiverWithCDC : IArchiver
    {
        public Task<HashSet<string>> Archive(HashSet<string> tables, bool keepVersions = false)
        {
            throw new NotImplementedException();
        }
    }
}
