﻿using System;

namespace SharpKit.Compiler.Utils
{
    static class EnumerableHelper
    {
        public static Type GetEnumerableItemType(object typedEnumerable)
        {
            var type = typedEnumerable.GetType();
            return GetEnumerableItemType(type);
        }
        public static Type GetEnumerableItemType(Type type)
        {
            var iface = type.GetInterface("System.Collections.Generic.IEnumerable`1");
            if (iface != null)
            {
                var args = iface.GetGenericArguments();
                return args[0];
            }
            return null;
        }
    }

}
