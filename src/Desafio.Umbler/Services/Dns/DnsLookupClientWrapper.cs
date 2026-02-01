using System.Linq;
using System.Threading.Tasks;
using DnsClient;

namespace Desafio.Umbler.Services.Dns
{
    public class DnsLookupClientWrapper : IDnsLookupClient
    {
        private readonly LookupClient _lookup = new LookupClient();

        public async Task<DnsLookupResult> LookupAsync(string domainName)
        {
            var result = await _lookup.QueryAsync(domainName, QueryType.ANY);
            var record = result.Answers.ARecords().FirstOrDefault();

            return new DnsLookupResult
            {
                Ip = record?.Address?.ToString(),
                Ttl = record?.TimeToLive ?? 0
            };
        }
    }
}
