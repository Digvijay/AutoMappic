using AutoMappic.Benchmarks;
using BenchmarkDotNet.Running;

var switcher = new BenchmarkSwitcher(new[] {
    typeof(MappingBenchmarks),
    typeof(ListMappingBenchmarks)
});

switcher.Run(args);
