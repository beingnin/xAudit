
namespace xAudit.Client.Console.FW
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using xAudit.Contracts;
    using xAudit.Infrastructure.Resolver;

    class Program
    {
        static async Task Main(string[] args)
        {
            IReplicator replicator = new Setup(
                @"Data Source=10.10.100.68\SQL2016;Initial Catalog=learns;User ID=spsauser;Password=$P$@789#",
                @"Data Source=10.10.100.68\SQL2016;Initial Catalog=learns;User ID=spsauser;Password=$P$@789#")
                             .UseCDC()
                             .ReplicateBeforeRecreation()
                             .ReplicateOnSchemaChanges()
                             .GetReplicator();
            try
            {

               await replicator.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            Console.ReadKey();
        }
    }
}
