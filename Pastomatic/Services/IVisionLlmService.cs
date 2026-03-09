using System.Threading;
using System.Threading.Tasks;

namespace Pastomatic.Services
{
    public interface IVisionLlmService
    {
        Task<string> DescribeImageAsync(byte[] imageBytes, CancellationToken ct = default);
    }
}
