using System;
using SharpKit.Compiler.Utils;

namespace SharpKit.Compiler.Java.Utils
{
    class JavaHelper
    {
        public static string ToJavaValue(object value)
        {
            if (value == null)
                return "null";
            string jsValue;
            if (value is string)
                jsValue = ToJavaString((string)value);
            else if (value is char)
                jsValue = ToJavaChar((char)value);
            else if (value is bool)
            {
                jsValue = ((bool)value == true) ? "true" : "false";
            }
            else if (value is DateTime)
            {
                DateTime date = (DateTime)value;
                jsValue = String.Format("new Date({0},{1},{2},{3},{4},{5},{6})", date.Year, date.Month - 1, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond);
                //jsValue = String.Format("new Date({0})", date.Ticks);
            }
            else if (value is decimal || value is float)
            {
                jsValue = value.ToString();
            }
            else if (value is Enum || !value.GetType().IsPrimitive)
                jsValue = String.Format("\"{0}\"", value);
            else
            {
                jsValue = value.ToString();
            }
            return jsValue;
        }

        public static string ToJavaString(string value)
        {
            if (value == null)
                return "null";
            var x = CSharpHelper.QuoteSnippetStringCStyle(value);
            return x;
        }

        public static string ToJavaChar(char ch)
        {
            var x = CSharpHelper.QuoteSnippetCharCStyle(ch);
            return x;
        }


    }
}
