namespace dnsclient
{
    public enum RecordType : ushort
    {
        None,
        /// <summary> Resolve 32-bit IP Address </summary>
        A,
        /// <summary> Specifies the name of a DNS name server that is authoritative for the zone. Each zone must have at least one NS record that points to its primary name server, and that name must also have a valid A (Address) record </summary>
        NS,
        MD,
        MF,
        /// <summary> The CNAME record provides a mapping between this alias and the “canonical” (real) name of the node. </summary> 
        CNAME,
        SOA,
        MB,
        MG,
        MR,
        NULL,
        WKS,
        PTR,
        HINFO,
        MINFO,
        /// <summary> Specifies the location (device name) that is responsible for handling e-mail sent to the domain. </summary>
        MX,
        /// <summary> Allows arbitrary additional text associated with the domain to be stored.  </summary>
        TXT,
        RP,
        AFSDB,
        X25,
        ISDN,
        RT,
        NSAP,
        NSAP_PTR,
        SIG,
        KEY,
        /// <summary> Returns a 128-bit IPv6 address, most commonly used to map hostnames to an IP address of the host.  </summary>
        AAAA = 28,
        LOC = 29,
        NXT = 30,
        SRV = 33,
        NAPTR = 35,
        KX = 36,
        A6 = 38,
        DNAME = 39,
        OPT = 41,
        DS = 43,
        SSHFP = 44,
        RRSIG = 46,
        NSEC = 47,
        DNSKEY = 48,
        NSEC3 = 50,
        NSEC3PARAM = 51,
        TLSA = 52,
        HIP = 55,
        CDS = 59,
        CDNSKEY = 60,
        OPENPGPKEY = 61,
        CSYNC = 62,
        ZONEMD = 63,
        SVCB = 64,
        HTTPS = 65,
        SPF = 99,
        EUI48 = 108,
        EUI64 = 109,
        TKEY = 249,
        TSIG = 250,
        IXFR = 251,
        AXFR = 252,
        MAILB = 253,
        MAILA = 254,
        /// <summary> All cached records.  </summary>
        All = 255,
        CAA = 257,
        AVC = 258,
        TA = 32768,
        DLV = 32769
    }





}
