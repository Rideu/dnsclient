using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using dnsclient.Utils;

namespace dnsclient.Utils
{
    public static class Helper
    {

        public static string GetExceptionRecur(this Exception e)
        {
            var msg = e;
            string buf = msg.Message + " ";
            while (msg != null)
            {
                msg = msg.InnerException;
                if (msg != null)
                    buf += msg.Message + " ";
            }
            return buf;
        }

        public static byte[] Rev(this byte[] src)
        {

            Array.Reverse(src);
            return src;
        }

        static Encoding asciiencoder = Encoding.ASCII;

        public static byte[] ASCIIEncode(this string s) => asciiencoder.GetBytes(s);
        public static int ASCIIEncode(this ArraySegment<char> s, Span<byte> buffer) => asciiencoder.GetBytes(s.AsSpan(), buffer);
        public static int ASCIIEncode(this ImmutableArray<char> r, Span<byte> buffer) => asciiencoder.GetBytes(r.AsSpan(), buffer);
        public static int ASCIIEncode(this ReadOnlySpan<char> r, Span<byte> buffer) => asciiencoder.GetBytes(r, buffer);
        public static string ASCIIDecode(this byte[] s) => asciiencoder.GetString(s);


        static Encoding urf8encoder = Encoding.UTF8;

        public static byte[] UTF8Encode(this string s) => urf8encoder.GetBytes(s);
        public static string UTF8Decode(this byte[] s) => urf8encoder.GetString(s);


        public static string Print<T>(this IEnumerable<T> collection, string div)
        {
            if (collection == null || collection.Count() == 0)
                return string.Empty;

            string buf = collection.First().ToString();
            var len = collection.Count();

            for (int i = 1; i < len; i++)
            {
                var el = collection.ElementAt(i);
                buf += div + el;
            }

            return buf;
        }

        public static int Count<T>(this IEnumerable<T> enumerable, T find, int startIndex = 0)
        {
            var len = enumerable.Count();
            var count = 0;

            for (; startIndex < len; startIndex++)
                if (enumerable.ElementAt(startIndex).Equals(find))
                    count++;
            return count;
        }

        public static int Count<T>(this ReadOnlySpan<T> span, T find, int startIndex = 0)
        {
            var len = span.Length;
            var count = 0;

            for (; startIndex < len; startIndex++)
                if (span[startIndex].Equals(find))
                    count++;
            return count;
        }

        public static ArraySegment<T>[] SegmentSplit<T>(this T[] array, T separator)
        {


            var divisions = array.Count(separator);

            var idx = 0;

            var from = 0;

            var result = new ArraySegment<T>[divisions + 1];

            for (int i = 0; i < array.Length; i++)

                if (array[i].Equals(separator))
                {
                    result[idx++] = new ArraySegment<T>(array, from, i - from);

                    from = i + 1;
                }

            result[idx] = new ArraySegment<T>(array, from, array.Length - from);

            return result;
        }

        public static ImmutableArray<T>[] Split<T>(this ReadOnlySpan<T> span, T separator)
        {

            var arr = span.ToImmutableArray();

            var divisions = arr.Count(separator);

            var idx = 0;

            var from = 0;

            var result = new ImmutableArray<T>[divisions + 1];

            for (int i = 0; i < span.Length; i++)

                if (span[i].Equals(separator))
                {
                    result[idx++] = span.Slice(from, i - from).ToImmutableArray();

                    from = i + 1;
                }

            result[idx] = span.Slice(from, span.Length - from).ToImmutableArray();

            return result;
        }

        public static ImmutableArray<ImmutableArray<T>> ImmutableSplit<T>(this ReadOnlySpan<T> span, T separator)
        {

            var arr = span.ToImmutableArray();

            var divisions = arr.Count(separator);

            var idx = 0;
            var from = 0;

            var result = new ImmutableArray<ImmutableArray<T>>()/*[divisions + 1]*/;

            ImmutableArray<T> selector(ImmutableArray<T> n) => imSelector(n, ref idx);
            //ImmutableArray.Create()
            return ImmutableArray.CreateRange(result, selector);

        }

        static ImmutableArray<T> imSelector<T>(ImmutableArray<T> r, ref int i)
        {
            return default;
        }
    }
}

namespace dnsclient
{
    [DebuggerDisplay("{FullName}")]
    public struct DomainName : IEnumerable<ArraySegment<char>>
    {
        public ArraySegment<char> this[int index]
        {
            get => Levels[index];
        }

        private char[] domainname;

        private string domainnamestring;

        public ArraySegment<char> Root { get; private set; }

        public int LevelsCount { get; private set; }

        public string FullNameString => domainnamestring = domainnamestring ?? new string(domainname);

        public char[] FullName => domainname;

        public ArraySegment<char>[] Levels { get; private set; }


        public DomainName(ArraySegment<char> fqdn) : this(fqdn.ToArray())
        {

        }

        public DomainName(string fqdn) : this(fqdn.ToCharArray())
        {
            domainnamestring = fqdn;
        }

        public DomainName(char[] fqdn)
        {

            domainname = fqdn;

            var span = fqdn.AsSpan();

            Levels = domainname.SegmentSplit('.')/*.Split('.')*/;

            LevelsCount = Levels.Length;

            Root = Levels[Levels.Length - 1];
        }

        public IEnumerator<ArraySegment<char>> GetEnumerator()
        {
            return ((IEnumerable<ArraySegment<char>>)Levels).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Levels.GetEnumerator();
        }

        public DomainName GetDomainLevels(byte levels)
        {
            var from = Levels[LevelsCount - levels];

            var seg = new ArraySegment<char>(domainname, from.Offset, domainname.Length - from.Offset);

            return new DomainName(seg);
        }

        public override string ToString()
        {
            return FullNameString;
        }
    }
}
