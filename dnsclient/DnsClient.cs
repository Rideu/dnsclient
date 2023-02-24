using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using dnsclient.Utils;

namespace dnsclient
{
    public static class DnsClient
    {
        static ushort IDIncrement = 1024;

        static DnsClient()
        {

        }

        public static Task<DnsResponse> ResolveNameAsync(DomainName hostname, RecordType reqtype = RecordType.A, bool secure = false, int dnsport = 53, List<DnsAlterMap> alters = null)
        {
            dynbitfield bf = bitfield_ctor(hostname, reqtype);

            var payload = bf.bytes.ToArray();


            if (secure)
            {
                var length = BitConverter.GetBytes((ushort)bf.bytes.Count).Rev(); // [5], 4.2.2. TCP usage

                var frame = new byte[2 + payload.Length];
                frame[0] = (byte)(bf.bytes.Count >> 8);
                frame[1] = (byte)(bf.bytes.Count >> 0);

                Array.Copy(payload, 0, frame, 2, payload.Length);

                TcpClient dnsClient = new TcpClient();
                dnsClient.Connect(IPAddress.Parse(SecureDNSAddress.Address), 853);

                var ns = dnsClient.GetStream();

                SslStream ssl = new SslStream(ns);
                ssl.AuthenticateAsClient(clientOpts);
                ssl.Write(frame, 0, frame.Length);

                byte[] recv = new byte[1024 * 16];
                var read = ssl.ReadAsync(recv, 0, recv.Length).ContinueWith((t) =>
                {
                    var size = BitConverter.ToUInt16(new byte[2] { recv[1], recv[0] }, 0);

                    if (size == 0)
                        throw new WebException("Empty response from the remote server", WebExceptionStatus.ReceiveFailure);

                    var raw = new byte[size];

                    for (int i = 0; i < raw.Length; i++)
                    {
                        raw[i] = recv[i + 2];
                    }

                    return DnsResponse.FromRaw(raw, alters);
                });

                return read;  
            }
            else
            {

                UdpClient dnsClient = new UdpClient();
                dnsClient.Connect(IPAddress.Parse(defaultDNSAddress), dnsport);

                var t = dnsClient.ReceiveAsync().ContinueWith<DnsResponse>((r) =>
                {
                    try
                    {

                        var udpr = r.Result;
                        var resp = DnsResponse.FromRaw(udpr.Buffer, alters);
                        return resp;
                    }
                    catch (Exception)
                    {
                        return default;
                    }
                });

                dnsClient.Send(payload, payload.Length);

                return t;
            }
        }

        static dynbitfield bitfield_ctor(DomainName hostname, RecordType reqtype = RecordType.A)
        {
            dynbitfield bf = new dynbitfield();
            bf.bytes = new List<byte>(14);


            for (int i = 0; i < 12; i++)
                bf.bytes.Add(0);

            // === HEADER START ===

            IDIncrement++;
            var id = BitConverter.GetBytes(IDIncrement).Rev();
            bf.bytes[0] = id[0];
            bf.bytes[1] = id[1];

            var isresponse = false;
            if (isresponse)
                bf.bytes[2] |= (byte)0b_1000_0000; // QR: 0 = query, 1 = response

            var istruncated = false;
            if (istruncated)
                bf.bytes[2] |= (byte)0b_0000_0010; // TC: 0 = not truncated, 1 = truncated

            var dorecursive = true;
            if (dorecursive)
                bf.bytes[2] |= (byte)0b_0000_0001; // RD: 0 = recursion not desired, 1 = else

            var Z = false;
            if (Z)
                bf.bytes[3] |= (byte)0b_0100_0000; // Z: reserved

            var nonauth = false;
            if (nonauth)
                bf.bytes[3] |= (byte)0b_0001_0000; // NA: 0 = don't send non-auth data, 1 = else

            var QCount = BitConverter.GetBytes((ushort)1).Rev(); // Questions Count [u16]
            bf.bytes[4] = QCount[0];
            bf.bytes[5] = QCount[1];

            var ANCount = BitConverter.GetBytes((ushort)0).Rev(); // Answer Record Count [u16]
            bf.bytes[6] = ANCount[0];
            bf.bytes[7] = ANCount[1];

            var NSCount = BitConverter.GetBytes((ushort)0).Rev(); // Authority Record Count [u16]
            bf.bytes[8] = NSCount[0];
            bf.bytes[9] = NSCount[1];

            var ARCount = BitConverter.GetBytes((ushort)0).Rev(); // Additional Record Count [u16]
            bf.bytes[10] = ARCount[0];
            bf.bytes[11] = ARCount[1];


            // === HEADER END ===


            // === QUESTION START ===

            var split = hostname;

            foreach (var s in split)
            {
                var len = (byte)s.Count;
                var buf = new byte[len];

                s.ASCIIEncode(buf);

                bf.bytes.Add(len);
                bf.bytes.AddRange(buf);
            }
            bf.bytes.Add(0);

            byte[] QType = BitConverter.GetBytes((ushort)reqtype).Rev();

            bf.bytes.AddRange(QType);

            byte QClass0 = 0;
            byte QClass1 = 1;

            byte[] QClass = new byte[2] { QClass0, QClass1 };

            bf.bytes.AddRange(QClass);

            // === QUESTION END ===

            return bf;
        }

        public static string defaultDNSAddress = NetworkInterface.GetAllNetworkInterfaces()?[0]?.GetIPProperties()?.DnsAddresses?[0]?.ToString() ?? "8.8.8.8";

        static SecureDNSInfo secureDNSInfo = new SecureDNSInfo { Host = "1dot1dot1dot1.cloudflare-dns.com", Address = "1.1.1.1" };
        public static SecureDNSInfo SecureDNSAddress { get => secureDNSInfo; set => updateSecureDNS(value); }

        static void updateSecureDNS(SecureDNSInfo dnsinfo)
        {
            secureDNSInfo = dnsinfo;

            clientOpts = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                TargetHost = secureDNSInfo.Host,
                RemoteCertificateValidationCallback = CheckDnsServerCertCallback,
            };
        }


        static SslClientAuthenticationOptions clientOpts = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            EncryptionPolicy = EncryptionPolicy.RequireEncryption,
            TargetHost = SecureDNSAddress.Host,
            RemoteCertificateValidationCallback = CheckDnsServerCertCallback,
        };

        public static bool VerifyCert { get; set; } = true;

        static bool CheckDnsServerCertCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {

                var c2 = certificate as X509Certificate2;

                if (!VerifyCert)
                    return true;

                if (c2 != null)
                {
                    var veresult = c2.Verify();
                    return veresult;
                }

                return true;
            }
            return false;
        }
    }

    public struct SecureDNSInfo
    {
        public string
            Host,
            Address;

    }
}
