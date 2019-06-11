using NLog;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace RavenDBBulkInsertTest
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            if (args.Length == 5)
            {
                var hostname = args[0];
                var databaseName = args[1];
                _logger.Info($"Using database {databaseName} on {hostname}");
                if (args[2] == "single")
                {
                    var ident = args[3];
                    var count = int.Parse(args[4], CultureInfo.InvariantCulture);
                    _logger.Info($"Running single import. Ident: {ident}, Count: {count}");
                    Task.Run(() => Import.Single.ImportSingleBatchAsync(hostname, databaseName, ident, count)).Wait();
                    return;
                }
                else if (args[2] == "parallel")
                {
                    var batches = int.Parse(args[3], CultureInfo.InvariantCulture);
                    var countPerBatch = int.Parse(args[4], CultureInfo.InvariantCulture);
                    _logger.Info($"Running parallel import. Batches: {batches}, Count per batch: {countPerBatch}");
                    Import.Parallel.ImportInParallel(hostname, databaseName, batches, countPerBatch);
                    return;
                }
            }

            _logger.Error("dotnet RavenDBBulkInsertTest.dll ServerURL DatabaseName single IDENT COUNT");
            _logger.Error("dotnet RavenDBBulkInsertTest.dll ServerURL DatabaseName parallel BATCHES COUNT_PER_BATCH");
        }
    }
}
