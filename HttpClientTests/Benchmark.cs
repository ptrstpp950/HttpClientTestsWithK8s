using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpClientTests
{
    public class BenchmarkResult
    {
        public double Max { get; set; }
        public double Min { get; set; }
        public double Avg { get; set; }
        public double Sum { get; set; }
    }
    
    public class Benchmark
    {
        public static async Task<BenchmarkResult> Run(Func<Task> func, int count)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            var results = new double[count];
            for (int i = 0; i < count; i++)
            {
                stopwatch.Restart();
                await func();
                stopwatch.Stop();
                results[i] = stopwatch.Elapsed.TotalMilliseconds;
            }

            var resultsList = results.ToList();
            return new BenchmarkResult()
            {
                Max = resultsList.Max(),
                Min = resultsList.Min(),
                Avg = resultsList.Average(),
                Sum = resultsList.Sum()
            };
        }
    }
}