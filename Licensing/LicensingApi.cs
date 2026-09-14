using Miautrix.Licensing.Client;

namespace ServiceTrayMonitor.Licensing
{
    /// <summary>The two licensing endpoints the app calls; an interface so tests can fake the server.</summary>
    public interface ILicensingApi
    {
        Task<(int HttpStatus, ApiResult Result)> ActivateAsync(ActivateRequest request, CancellationToken ct = default);
        Task<(int HttpStatus, ApiResult Result)> CheckinAsync(CheckinRequest request, CancellationToken ct = default);
    }

    /// <summary>Adapter over the vendor <see cref="ActivationApiClient"/> (kept unmodified in Licensing/Client).</summary>
    public sealed class LicensingApi : ILicensingApi, IDisposable
    {
        private readonly ActivationApiClient _client;

        public LicensingApi(string baseUrl) => _client = new ActivationApiClient(baseUrl);

        public Task<(int HttpStatus, ApiResult Result)> ActivateAsync(ActivateRequest request, CancellationToken ct = default) =>
            _client.ActivateAsync(request, ct);

        public Task<(int HttpStatus, ApiResult Result)> CheckinAsync(CheckinRequest request, CancellationToken ct = default) =>
            _client.CheckinAsync(request, ct);

        public void Dispose() => _client.Dispose();
    }
}
