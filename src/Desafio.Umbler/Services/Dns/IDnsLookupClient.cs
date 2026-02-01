using System.Threading.Tasks;

namespace Desafio.Umbler.Services.Dns
{
    public class DnsLookupResult
    {
        public string Ip { get; set; }
        public int Ttl { get; set; }
    }

    public interface IDnsLookupClient
    {
        Task<DnsLookupResult> LookupAsync(string domainName);
    }
}
