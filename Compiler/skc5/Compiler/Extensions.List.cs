using System;
using System.Collections.Generic;

namespace SharpKit.Compiler
{
    static class ListExtensions
    {
        //public static void RemoveDoubles<T>(this List<T> list, T item) where T : class
        //{
        //    var i = 0;
        //    var count = 0;
        //    while (i < list.Count)
        //    {
        //        var item2 = list[i];
        //        if (item2 == item)
        //        {
        //            count++;
        //            if (count > 1)
        //            {
        //                list.RemoveAt(i);
        //                continue;
        //            }
        //        }
        //        i++;
        //    }
        //}

        public static void RemoveDoubles<T>(this List<T> list, Func<T, bool> selector) where T : class
        {
            var i = 0;
            var count = 0;
            while (i < list.Count)
            {
                var item2 = list[i];
                if (selector(item2))
                {
                    count++;
                    if (count > 1)
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                }
                i++;
            }
        }
        public static void RemoveDoublesByKey<K, T>(this List<T> list, Func<T, K> keySelector)
            where T : class
            where K : class
        {
            var set = new HashSet<K>();
            var i = 0;
            while (i < list.Count)
            {
                var item = list[i];
                var key = keySelector(item);
                if (key != null)
                {
                    if (set.Contains(key))
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                    else
                    {
                        set.Add(key);
                    }
                }
                i++;
            }
        }

    }
}
