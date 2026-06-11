namespace GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;

/// <summary>
/// D23. История платежей текущего пользователя. UserId берётся из JWT.
/// </summary>
public sealed record GetMyPaymentsQuery(int Page = 1, int PageSize = 20);
