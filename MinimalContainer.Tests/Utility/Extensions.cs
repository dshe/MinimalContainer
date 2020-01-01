using Microsoft.Extensions.Logging;
using System;



namespace MinimalContainer.Tests.Utility
{
    public static class Extensions
    {
        public static Exception WriteMessageTo(this Exception ex, ILogger logger)
        {
            logger.LogDebug(ex.Message);
            if (ex.InnerException != null)
                logger.LogDebug(ex.InnerException.Message);
            logger.LogDebug("");
            return ex;
        }
    }
}
