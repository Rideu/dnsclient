namespace dnsclient
{ 
    public struct DnsQuestion
    {
        public byte Offset;
        public string Hostname;
        public RecordType Type;
        public ushort Class;

        public override string ToString()
        {
            return Hostname;
        }
    } 
}
