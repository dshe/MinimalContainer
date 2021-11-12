using System;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Utility
{
    public abstract class BaseUnitTest
    {
        protected readonly Action<string> Log;

        public BaseUnitTest(ITestOutputHelper output)
        {
            Log = output.WriteLine;
        }
    }
}
