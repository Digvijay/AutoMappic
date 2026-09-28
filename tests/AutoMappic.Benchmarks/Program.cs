using AutoMappic.Benchmarks;
using BenchmarkDotNet.Running;

var switcher = new BenchmarkSwitcher([
    typeof(MappingBenchmarks),
    typeof(ListMappingBenchmarks)
]);

switcher.Run(args);
