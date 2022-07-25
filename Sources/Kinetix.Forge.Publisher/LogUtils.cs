using System;
using log4net;

namespace Kinetix.Forge.Publisher
{
    public static class LogUtils
    {
        private static readonly ILog _log = LogManager.GetLogger("Publisher");

        public static void Info(string s = "")
        {
            _log.Info(s);
            Console.Out.WriteLine(s);
        }
    }
}
