namespace dnsclient
{
    public struct DnsAuthority
    {
        public byte Offset;
        public string Hostname;
        public ushort Type;
        public ushort Class;

        public override string ToString()
        {
            return Hostname;
        }
    }
}
