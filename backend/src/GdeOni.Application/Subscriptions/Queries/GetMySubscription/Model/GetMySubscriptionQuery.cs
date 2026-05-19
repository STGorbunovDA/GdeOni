namespace GdeOni.Application.Subscriptions.Queries.GetMySubscription.Model;

/// <summary>
/// D16. Query без параметров — userId берётся из JWT через
/// <c>ICurrentUserService</c>.
/// </summary>
public sealed record GetMySubscriptionQuery;
