using System;
using Xunit.Abstractions;

namespace Testing.Utility
{
    public class TestBase
    {
        protected readonly Action<string> Write;
        public TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;
        }
    }
}
