using System.Collections.Generic;
using System.Threading.Tasks;


namespace xAudit.Contracts
{
    public interface IArchiver
    {
        Task<HashSet<string>> Archive(HashSet<string> tables,string instanceName, bool keepVersions=false);
    }
}
