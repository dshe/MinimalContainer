using System;

namespace InternalContainerTests.Utilities
{
    public static class TestExtensionMethods
    {
        public static Exception Output(this Exception ex, Action<string> write)
        {
            write(ex.Message);
            return ex;
        }
    }
}
