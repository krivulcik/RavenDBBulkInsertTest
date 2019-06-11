using NLog;
using Raven.Client.Documents;
using RavenDBBulkInsertTest.Model;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RavenDBBulkInsertTest.Import
{
    public static class Single
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static async Task ImportSingleBatchAsync(string hostname, string databaseName, string ident, int count)
        {
            var documentStore = new DocumentStore()
            {
                Urls = new[] { hostname },
                Database = databaseName,
            }.Initialize();

            _logger.Info("Start.");
            var sw = Stopwatch.StartNew();
            var employees = Enumerable.Range(0, count)
                .Select(i => new Employee { Id = "employee/" + ident + "-" + i, FirstName = i.ToString(), LastName = i.ToString() })
                .ToArray();
            sw.Stop();
            _logger.Info($"Data prepared in {sw.ElapsedMilliseconds} ms.");

            sw.Restart();
            using (var bulkInsert = documentStore.BulkInsert())
            {
                foreach (var employee in employees)
                {
                    await bulkInsert.StoreAsync(employee);
                }
            }
            sw.Stop();

            _logger.Info($"Inserted {employees.Length} employees in {sw.Elapsed.TotalSeconds:N2} seconds.");
            _logger.Info($"{employees.Length / sw.Elapsed.TotalSeconds:N2} records/s");
        }
    }
}
