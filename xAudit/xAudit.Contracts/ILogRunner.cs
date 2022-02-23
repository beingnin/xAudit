using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using xAudit.CDC.Shared;

namespace xAudit.Contracts
{
    public interface ILogRunner
    {
        Task<int> CreateRun(Run run);
        Task<int> Log(Log log);
    }
}
