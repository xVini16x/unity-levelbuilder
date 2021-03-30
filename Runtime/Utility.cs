using System;
using System.Collections.Generic;

namespace UnityLevelEditor
{
    public static class Utility
    {
        /// <summary>
        /// Selects a random element from a <see cref="IList{T}"/>.
        /// </summary>
        /// <param name="list">The <see cref="IList{T}"/> to select from.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="list"/>.</typeparam>
        /// <returns>A random element from the list.</returns>
        /// <exception cref="ArgumentException"><paramref name="list"/> is null or empty.</exception>
        public static TSource PickRandom<TSource>(this IList<TSource> list)
        {
            if (list == null) { throw new ArgumentNullException(nameof(list), $"{nameof(list)} is null"); }
            if (list.Count == 0) { throw new ArgumentException($"Cannot select a random element from an empty {nameof(IList<TSource>)}.", nameof(list)); }
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
    }
}
