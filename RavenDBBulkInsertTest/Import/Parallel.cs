using MoreLinq;
using NLog;
using Raven.Client.Documents;
using RavenDBBulkInsertTest.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RavenDBBulkInsertTest.Import
{
    public static class Parallel
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void ImportInParallel(string hostname, string databaseName, string ident, int batches, int countPerBatch)
        {
            var store = new DocumentStore()
            {
                Urls = new[] { hostname },
                Database = databaseName,
            }.Initialize();

            store.Maintenance.Send(new Raven.Client.Documents.Operations.GetStatisticsOperation());

            _logger.Info("Start.");
            var sw = Stopwatch.StartNew();
            var employees = Enumerable.Range(0, batches)
                .ToDictionary(
                    x => x.ToString(),
                    x => Enumerable.Range(0, countPerBatch)
                        .Select(i => new Employee { Id = "employee/" + ident + "-" + x.ToString() + "-" + i.ToString(), FirstName = i.ToString(), LastName = i.ToString() })
                        .ToArray());
            _logger.Info($"Data prepared in {sw.ElapsedMilliseconds} ms.");

            var threads = new Dictionary<string, Thread>();

            foreach (var batch in employees)
            {
                threads[batch.Key] = new Thread(() => ImportBatch(store, batch.Key, batch.Value));
                threads[batch.Key].Start();
            }

            sw.Restart();
            foreach (var thread in threads)
            {
                thread.Value.Join();
            }

            sw.Stop();

            _logger.Info($"Inserted {employees.Sum(x => x.Value.Length)} employees in {sw.Elapsed.TotalSeconds:N2} seconds.");
            _logger.Info($"{employees.Sum(x => x.Value.Length) / sw.Elapsed.TotalSeconds:N2} records/s");
        }

        public static void ImportBatch(IDocumentStore documentStore, string ident, Employee[] employees)
        {
            _logger.Info("Starting batch: " + ident);
            using (var bulkInsert = documentStore.BulkInsert())
            {
                foreach (var employee in employees)
                {
                    bulkInsert.Store(employee);
                }
            }
            _logger.Info("Finished batch: " + ident);
        }
    }
}
