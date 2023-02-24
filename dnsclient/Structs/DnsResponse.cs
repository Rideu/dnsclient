
using System;
using System.Collections.Generic;
using System.Linq;

namespace dnsclient
{


    public partial struct DnsResponse
    {
        public ushort id;
        public bool isResponse;
        public bool recursiveAvailable;

        public List<DnsQuestion> Requests;
        public List<DnsAnswer> Answers;
        public List<DnsAuthority> Authorities;

        public byte[] raw;



        private static ushort toUShort(byte[] raw, int a, int b)
        {
            return (ushort)(raw[a] << 8 | raw[b]);
        }

        public static DnsResponse FromRaw(byte[] raw, IEnumerable<DnsAlterMap> alters = null)
        {
            DnsResponse respFrame = default;


            respFrame.id = toUShort(raw, 0, 1);

            respFrame.isResponse = (raw[2] & 0b_1000_0000) == 128;
            respFrame.recursiveAvailable = (raw[3] & 0b_1000_0000) == 128;

            var QSCount = toUShort(raw, 4, 5);
            var ANCount = toUShort(raw, 6, 7);
            var NSCount = toUShort(raw, 8, 9);
            var ARCount = toUShort(raw, 10, 11);

            int i = 12;
            var questions = new List<DnsQuestion>();
            var buf = "";
            byte qoffset = (byte)i;

            if (QSCount > 0)
                while (true)
                {
                    var len = raw[i];

                    if (len == 0)
                    {
                        questions.Add(new DnsQuestion
                        {
                            Offset = qoffset,
                            Hostname = buf.Trim('.'),
                            Type = (RecordType)toUShort(raw, i + 1, i + 2),
                            Class = toUShort(raw, i + 3, i + 4),
                        });
                        buf = "";
                        i += 4;
                        qoffset = (byte)i;
                        if (questions.Count == QSCount)
                            break;
                        else continue;
                    }

                    for (int c = 0; c < len; c++)
                    {
                        i++;
                        buf += (char)(raw[i]);
                    }
                    buf += '.';
                    //i += len;

                    i++;
                }

            i++;
            var dnsAnswers = new List<DnsAnswer>();

            if (ANCount > 0)
                while (true)
                {

                    var isptr = (raw[i + 0] & 0b_1100_0000) == 192;
                    string host = null;
                    if (isptr)
                    {
                        var offset = raw[i + 1];
                        host = questions.FirstOrDefault(n => n.Offset == offset).Hostname;

                        if (host == null)
                            host = dnsAnswers.FirstOrDefault(n => n.Offset == offset).Payload;
                    }

                    ushort AName = toUShort(raw, i + 0, i + 1);
                    i += 2;
                    RecordType AType = (RecordType)toUShort(raw, i + 0, i + 1);
                    i += 2;
                    ushort AClass = toUShort(raw, i + 0, i + 1);
                    i += 2;
                    uint ATTL = toUShort(raw, i + 0, i + 1);
                    i += 4;

                    ushort ALength = toUShort(raw, i + 0, i + 1);
                    i += 2;

                    ushort APreference = 0;

                    var APayload = "";

                    byte len = 0;
                    var aoffset = i;
                    byte[] addrBuf = null;

                    DnsAnswer danswer = default;

                    if (AType == RecordType.A && ALength == 4) // => A-record && IPv4 address
                    {
                        var alt = alters?.FirstOrDefault(n => host?.Contains(n.domainName) ?? false);

                        addrBuf = new byte[4];
                        for (int c = 0; c < ALength; c++)
                        {
                            if (alt?.domainName != null)
                            {
                                raw[i + c] = alt.Value.ipAddress[c];
                            }
                            addrBuf[c] = raw[i + c];
                        }
                        danswer.Address = addrBuf;
                    }
                    else
                    if (AType == RecordType.CNAME || AType == RecordType.MX) // => CNAME || MX
                    {
                        int c = 0;
                        int shift = 0;
                        if (AType == RecordType.MX)
                        {
                            APreference = toUShort(raw, i + 0, i + 1);
                            i += 2;
                            shift = 2;
                        }
                        for (; c < ALength - shift; c++)
                        {
                            var b = raw[i + c];
                            if ((b) == 192)
                            {
                                var idx = raw[i + c + 1];
                                APayload += '.' + extractbyoffset(raw, idx);
                                c += 1;
                            }
                            else
                            if (len == 0)
                            {
                                len = b;
                                if (APayload.Length > 0) APayload += '.';
                            }
                            else
                            {
                                APayload += (char)b;
                                len--;
                            }
                        }
                    }
                    else
                    if (AType == RecordType.TXT) // => TXT
                    {
                        int c = 0;
                        int shift = 0;
                        //var textLength = raw[i + 0];
                        //i += 1;
                        shift = 0;
                        for (; c < ALength - shift; c++)
                        {
                            var b = raw[i + c];
                            if ((b) == 192)
                            {
                                var idx = raw[i + c + 1];
                                APayload += '.' + extractbyoffset(raw, idx);
                                c += 1;
                            }
                            else
                            if (len == 0)
                            {
                                len = b;
                                //if (APayload.Length > 0) APayload += '.';
                            }
                            else
                            {
                                APayload += (char)b;
                                len--;
                            }
                        }
                    }
                    else
                    if (AType == RecordType.HTTPS)
                    {

                    }
                    APayload = APayload.Trim('.');

                    i += ALength - (AType == RecordType.MX ? 2 : 0);

                    danswer.Name = host;
                    danswer.Type = AType;
                    danswer.Class = AClass;
                    danswer.TTL = ATTL;
                    danswer.Length = ALength;
                    danswer.Payload = string.IsNullOrEmpty(APayload) ? null : APayload;
                    danswer.Preference = APreference;
                    danswer.Offset = aoffset;


                    dnsAnswers.Add(danswer);

                    if (dnsAnswers.Count == ANCount)
                        break;
                }


            var authorities = new List<DnsAuthority>();

            if (NSCount > 0)
            {

            }

            respFrame.Requests = questions;
            respFrame.Answers = dnsAnswers;
            respFrame.Authorities = authorities;
            respFrame.raw = raw;

            return respFrame;
        }

        static string extractbyoffset(byte[] src, int from)
        {
            var buf = "";
            var len = src[from];
            var i = 1;
            byte b = 0;
            while (len >= 0 && len < 64)
            {
                b = src[i + from];

                if (b == 192)
                {
                    buf += '.' + extractbyoffset(src, src[i + from + 1]);
                }
                else
                if (len == 0)
                {
                    if (b > 0 && b < 64)
                    {

                        buf += '.';
                        len = (byte)(b + 1); // +1 will be substracted
                    }
                    else
                        break;
                }
                else
                    buf += (char)b;
                len--;
                i++;

            }

            return buf;
        }
    }


}
