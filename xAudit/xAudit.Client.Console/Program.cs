using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xAudit.Contracts;
using xAudit.Infrastructure.Resolver;

namespace xAudit.Client.Console.FW
{
    class Program
    {
        static void Main(string[] args)
        {
            IReplicator replicator = new Setup("","")
                             .UseCDC()
                             .ReplicateBeforeRecreation()
                             .ReplicateOnSchemaChanges()
                             .GetReplicator();
            replicator.StartAsync();
        }
    }
}
