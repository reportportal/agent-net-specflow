using System.Collections.Concurrent;

namespace ReportPortal.SpecFlowPlugin
{
    internal static class LockHelper
    {
        private static readonly ConcurrentDictionary<int, object> Repository = new ConcurrentDictionary<int, object>();

        private static readonly object GetLockLock = new object();

        public static object GetLock(int hashCode)
        {
            lock (GetLockLock)
            {
                if (Repository.ContainsKey(hashCode))
                {
                    return Repository[hashCode];
                }

                var lockObj = new object();
                Repository.AddOrUpdate(hashCode, lockObj, (key, oldValue) => oldValue);

                return lockObj;
            }
        }
    }
}
