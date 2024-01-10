#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using static SRStatEditor.Program;

namespace SRStatEditor
{
    public static class Extensions
    {
        public static IEnumerable<string> MergeWhitespace(this IEnumerable<string> source)
        {
            var spaces = 0;
            var it = source.GetEnumerator();
            while (it.MoveNext())
            {
                if (it.Current!.Length == 0)
                {
                    spaces++;
                    continue;
                }
                // Very bad case for merging entries - one value needs averaging
                // (actually, just to stay same), second value needs summation.
                // This is clutch, but best one I could think of:
                // just merge first value into line entry.
                if (it.Current!.Equals("$Tourism_SpendUSD")
                    || it.Current!.Equals("$Tourism_SpendRUB"))
                {
                    var newEntry = it.Current + " ";
                    if (!it.MoveNext())
                        throw new ArgumentException("This actually should never be thrown... Something is very wrong in stats.ini file!");
                    yield return newEntry + it.Current;
                    continue;
                }
                yield return new String(' ', spaces) + it.Current;
                break;
            }
            while (it.MoveNext())
                yield return it.Current!;
            it.Dispose();
        }
        public delegate void RefAction<T>(ref T value);

        public static void ForEach<T>(this T[] array, RefAction<T> action)
        {
            var it = array.GetEnumerator(); // just to ensure array has not changed.
            for (int i = 0; i < array.Length && it.MoveNext(); i++)
            {
                action(ref array[i]);
            }
        }
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            var it = source.GetEnumerator();
            bool hasRemainingItems = false;
            bool isFirst = true;
            T item = default(T);

            do
            {
                hasRemainingItems = it.MoveNext();
                if (hasRemainingItems)
                {
                    if (!isFirst)
                        yield return item;
                    item = it.Current;
                    isFirst = false;
                }
            } while (hasRemainingItems);
            it.Dispose();
        }
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector)
        {
            return enumerable.GroupBy(keySelector).Select(grp => grp.First());
        }
    }
}
