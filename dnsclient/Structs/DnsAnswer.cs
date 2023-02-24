using System.Linq;

namespace dnsclient
{
    public struct DnsAnswer
    {
        public int Offset;
        public string Name;
        public RecordType Type;
        public ushort Class;
        public uint TTL;
        public ushort Length;
        public string Payload;
        public byte[] Address;
        internal ushort Preference;

        public string IPString()
        {
            return _ipstring = _ipstring ?? Address.Aggregate("", (s, v) => s += v.ToString() + '.').TrimEnd('.');
        }

        string _ipstring;

        public override string ToString()
        {
            return Payload = Payload ?? Address.Aggregate("", (s, v) => s += v.ToString() + '.').TrimEnd('.');
        }
    }
}
