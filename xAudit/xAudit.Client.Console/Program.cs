
namespace xAudit.Client.Console.FW
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using xAudit;

    class Program
    {
        static async Task Main(string[] args)
        {
            var tables = new AuditTableCollection()
            {
                { 
                    "dbo",new string[]
                    { 
                        "products",
                        "groups"
                    }
                },
                {

                    "ofc",new string[]
                    {
                        "mails",
                        "senders",
                        "priorities"

                    }
                }
            };

            IReplicator replicator = new Setup(
                @"Data Source=10.10.100.68\SQL2016;Initial Catalog=learns;User ID=spsauser;Password=$P$@789#",
                @"Data Source=10.10.100.68\SQL2016;Initial Catalog=learns;User ID=spsauser;Password=$P$@789#")
                             .UseCDC()
                             .TrackSchemaChanges()
                             .EnablePartition()
                             .SetInstanceName("xAudit")
                             .Tables(tables)
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
            Console.WriteLine("Ran completely");
            Console.ReadKey();
        }
    }
}
