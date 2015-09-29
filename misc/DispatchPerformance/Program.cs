namespace DispatchPerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            PerfDispatchTest.Run();
            // PerfInvokeTest.Run();
            PerfRequestTest.Run();
        }
    }
}
