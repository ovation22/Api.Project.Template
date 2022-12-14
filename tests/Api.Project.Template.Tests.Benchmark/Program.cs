using BenchmarkDotNet.Running;

namespace Api.Project.Template.Tests.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
