using System;
using System.Text;
using System.Globalization;

namespace SharpKit.Compiler.Utils
{
    public class CSharpHelper
    {
        public static string QuoteSnippetCharCStyle(char value)
        {
            return QuoteSnippetStringCStyle(value.ToString(), "\'");    
        }

        public static string QuoteSnippetStringCStyle(string value)
        {
            return QuoteSnippetStringCStyle(value, "\"");
        }

        public static string QuoteSnippetStringCStyle(string value, string quoteChar)
        {
            StringBuilder b = new StringBuilder(value.Length + 5);

            b.Append(quoteChar);

            int i = 0;
            while (i < value.Length)
            {
                switch (value[i])
                {
                    case '\r':
                        b.Append("\\r");
                        break;
                    case '\t':
                        b.Append("\\t");
                        break;
                    case '\"':
                        b.Append("\\\"");
                        break;
                    case '\'':
                        b.Append("\\\'");
                        break;
                    case '\\':
                        b.Append("\\\\");
                        break;
                    case '\0':
                        b.Append("\\0");
                        break;
                    case '\n':
                        b.Append("\\n");
                        break;
                    case '\u2028':
                    case '\u2029':
                        AppendEscapedChar(b, value[i]);
                        break;

                    default:
                        b.Append(value[i]);
                        break;
                }
                ++i;
            }

            b.Append(quoteChar);

            return b.ToString();
        }

        private static void AppendEscapedChar(StringBuilder b, char value)
        {
            b.Append("\\u");
            b.Append(((int)value).ToString("X4", CultureInfo.InvariantCulture));
        }
       
        
    }
}
