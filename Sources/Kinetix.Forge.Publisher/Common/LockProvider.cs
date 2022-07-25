using System.Collections.Generic;

namespace Kinetix.Forge.Publisher.Common
{
    /// <summary>
    /// Fournit des lock pour une collection arbitraire de clé.
    /// Les clées sont sérialisés en string.
    /// </summary>
    public class LockProvider
    {
        /// <summary>
        /// Lock de l'instance de LockProvider pour l'accès à son dictionnaire de lock.
        /// </summary>
        private readonly object _mapLock = new object();

        private readonly Dictionary<string, object> _lockMap = new Dictionary<string, object>();

        /// <summary>
        /// Obtient un lock pour une clé donnée.
        /// </summary>
        /// <param name="itemKey">Clé de l'item.</param>
        /// <returns>Lock de l'item.</returns>
        public object GetLock(object itemKey)
        {
            lock (_mapLock)
            {
                string key = itemKey.ToString();
                if (_lockMap.TryGetValue(key, out object itemLock))
                {
                    return itemLock;
                }

                return _lockMap[key] = itemLock = new object();
            }
        }
    }
}
