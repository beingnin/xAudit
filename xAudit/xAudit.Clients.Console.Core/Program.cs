
namespace xAudit.Clients.Console.Core
{
    using System;
    using System.Threading.Tasks;
    using xAudit.Contracts;
    using xAudit.Infrastructure.Resolver;

    class Program
    {
        static async Task Main(string[] args)
        {
            IReplicator replicator = new Setup(
               @"Data Source=10.10.100.68\SQL2016;Initial Catalog=SharjahPolice_Live_Beta_New;User ID=spsauser;Password=$P$@789#",
               @"Data Source=10.10.100.68\SQL2016;Initial Catalog=SharjahPolice_Live_Beta_New;User ID=spsauser;Password=$P$@789#")
                            .UseCDC()
                            .ReplicateBeforeRecreation()
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
