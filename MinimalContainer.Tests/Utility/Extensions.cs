using System;

namespace MinimalContainer.Tests.Utility
{
    public static class Extensions
    {
        public static Exception WriteMessageTo(this Exception ex, Action<string> write)
        {
            write(ex.Message);
            if (ex.InnerException != null)
                write(ex.InnerException.Message);
            write("");
            return ex;
        }
    }
}
