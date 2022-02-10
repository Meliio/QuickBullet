using System.Net;
using Yove.Proxy;

namespace QuickBullet.Models
{
    public class Proxy : ProxyClient
    {
        public new ICredentials Credentials 
        {
            set
            {
                var uri = new Uri($"{Type}://{Host}:{Port}");
                Username = value.GetCredential(uri, uri.Scheme)?.UserName ?? string.Empty;
                Password = value.GetCredential(uri, uri.Scheme)?.Password ?? string.Empty;
                base.Credentials = value;
            }
        }

        public string Host { get; } = string.Empty;
        public string Port { get; } = string.Empty;
        public string Type { get; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool IsValid { get; set; } = true;

        public Proxy(string host, int port, ProxyType type) : base(host, port, type)
        {
            Host = host;
            Port = port.ToString();
            Type = type.ToString();
        }

        public override string ToString()
        {
            return string.Join(':', Host, Port);
        }
    }
}
