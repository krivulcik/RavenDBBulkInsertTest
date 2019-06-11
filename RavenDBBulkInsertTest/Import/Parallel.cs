using MoreLinq;
using NLog;
using Raven.Client.Documents;
using RavenDBBulkInsertTest.Model;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RavenDBBulkInsertTest.Import
{
    public static class Parallel
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void ImportInParallel(string hostname, string databaseName, int batches, int countPerBatch)
        {
            _logger.Info("Start.");
            var sw = Stopwatch.StartNew();
            var employees = Enumerable.Range(0, batches)
                .ToDictionary(
                    x => x.ToString(),
                    x => Enumerable.Range(0, countPerBatch)
                        .Select(i => new Employee { Id = "employee/" + x.ToString() + "-" + i.ToString(), FirstName = i.ToString(), LastName = i.ToString() })
                        .ToArray());
            _logger.Info($"Data prepared in {sw.ElapsedMilliseconds} ms.");

            sw.Restart();

            var barrier = new Barrier(employees.Count + 1);

            var threads = employees
                .ToDictionary(x => x.Key, x =>
                {
                    var thread = new Thread(new ThreadStart(async () => { await ImportBatch(hostname, databaseName, x.Key, x.Value); barrier.SignalAndWait(); }));
                    thread.Start();
                    return thread;
                });

            barrier.SignalAndWait();
            sw.Stop();

            _logger.Info($"Inserted {employees.Sum(x => x.Value.Length)} employees in {sw.Elapsed.TotalSeconds:N2} seconds.");
            _logger.Info($"{employees.Sum(x => x.Value.Length) / sw.Elapsed.TotalSeconds:N2} records/s");
        }

        public static async Task ImportBatch(string hostname, string databaseName, string ident, Employee[] employees)
        {
            var documentStore = new DocumentStore()
            {
                Urls = new[] { hostname },
                Database = databaseName,
            }.Initialize();

            _logger.Info("Starting batch: " + ident);
            using (var bulkInsert = documentStore.BulkInsert())
            {
                foreach (var employee in employees)
                {
                    await bulkInsert.StoreAsync(employee);
                }
            }
            _logger.Info("Finished batch: " + ident);
        }
    }
}
