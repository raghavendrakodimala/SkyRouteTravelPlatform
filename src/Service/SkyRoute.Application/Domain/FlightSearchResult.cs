namespace SkyRoute.Application.Domain;

/// <summary>
/// Outcome of a multi-provider flight-search aggregation (AUD-038). Carries the merged flight
/// results together with a total-outage signal so the API can distinguish:
/// <list type="bullet">
///   <item>a genuine no-match — every provider ran successfully but returned zero flights
///   (e.g. the DEC-021 MAN&lt;-&gt;SYD no-service route) → HTTP 200 with an empty list; from</item>
///   <item>a total provider outage — at least one provider was registered and EVERY one of
///   them failed → HTTP 503, so the UI can prompt a retry and monitoring can alert.</item>
/// </list>
/// Partial failure (at least one provider succeeded) preserves fault isolation and still
/// returns 200 with the available results, so <see cref="AllProvidersFailed"/> is only true
/// when the failure is total.
/// </summary>
public sealed record FlightSearchResult(IReadOnlyList<FlightResult> Flights, bool AllProvidersFailed);
