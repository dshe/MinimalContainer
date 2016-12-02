using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Utility
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
