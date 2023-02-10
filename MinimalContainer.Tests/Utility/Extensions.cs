using System;

namespace MinimalContainer.Tests.Utility
{
    public static class Extensions
    {
        public static Exception WriteMessageTo(this Exception ex, Action<string> log)
        {
            log.Invoke(ex.Message);
            if (ex.InnerException != null)
                log.Invoke(ex.InnerException.Message);
            log.Invoke("");
            return ex;
        }
    }
}
