using System.Collections.Generic;

namespace Breeze.NHibernate.Extensions
{
    internal static class ReadOnlyCollectionExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> collection, T item)
        {
            for (var i = 0; i < collection.Count; i++)
            {
                if (Equals(collection[i], item))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
