
namespace xAudit.Clients.Console.Core
{
    using System;
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
                        "groups",
                        "products"
                    }
                },
            };
            IReplicator replicator = new Setup(@"Data Source=10.10.100.68\SQL2016;Initial Catalog=learns;User ID=spsauser;Password=$P$@789#")
                            .UseCDC()
                            //.DoNotTrackSchemaChanges()
                            //.DisablePartition()
                            .Directory(@"C:\Users\Public")
                            //.SetInstanceName("history")
                            .AllowDataLoss()
                            .Tables(tables)
                            .GetReplicator();
            try
            {

                await replicator.StartAsync();
                Console.WriteLine("Completed run");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            Console.ReadKey();
        }
    }
}
