using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Benchmark
{
    internal class GFlopsColumn : IColumn
    {
        public string Id => "GFlops";

        public string ColumnName => "GFlops";

        public bool AlwaysShow => true;

        public ColumnCategory Category => ColumnCategory.Metric;

        public int PriorityInCategory => 0;

        public bool IsNumeric => true;

        public UnitType UnitType => UnitType.Dimensionless;

        public string Legend => "Giga Floating Point Operations Per Second";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        => GetValue(summary, benchmarkCase, benchmarkCase.Config.SummaryStyle);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            return string.Create(style.CultureInfo, $"{GetGFlops(summary, benchmarkCase):F3} GFlops");
        }
        public static double GetGFlops(Summary summary, BenchmarkCase benchmarkCase)
        {
            int length = (int)benchmarkCase.Parameters["Length"];

            double totalFpOps = 2L * length * length * length;
            var statistics = summary[benchmarkCase]?.ResultStatistics!;
            return totalFpOps / statistics?.Mean ?? 0;
        }

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => true;
    }
}
