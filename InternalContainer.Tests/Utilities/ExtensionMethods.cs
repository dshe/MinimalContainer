using System;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Utilities
{
    public static class TestExtensionMethods
    {
        public static Exception Output(this Exception ex, ITestOutputHelper output)
        {
            output.WriteLine(ex.Message);
            return ex;
        }
    }
}
