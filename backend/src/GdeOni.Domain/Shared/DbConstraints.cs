namespace GdeOni.Domain.Shared;

public static class DbConstraints
{
    public const string DeceasedSearchKey = "ux_deceased_search_key";
    public const string UxUsersEmail = "ux_users_email";
    public const string UxUsersName = "ux_users_user_name";
    public const string UxRefreshTokensTokenHash = "ux_refresh_tokens_token_hash";
    public const string UxDeceasedMediaStorageKey = "ux_deceased_media_storage_key";
    public const string UxSubscriptionPaymentsExternalPaymentId = "ux_subscription_payments_external_payment_id";
    public const string UxTrackedDeceasedUserIdDeceasedId = "ux_tracked_deceased_user_id_deceased_id";
    // D37. Дедуп-ключ разосланных писем о годовщинах: одно письмо на
    // (пользователь, умерший, тип годовщины, дата годовщины).
    public const string UxSentAnniversaryEmails = "ux_sent_anniversary_emails_user_deceased_kind_date";

    // Одна настройка напоминания на пару (пользователь, праздник по ключу-имени).
    public const string UxHolidayRemindersUserKey = "ux_holiday_reminders_user_id_holiday_key";

    // D46. Уникальный короткий код ссылки «поделиться подборкой».
    public const string UxShareBundlesCode = "ux_share_bundles_code";

    // Функция «Родственники»: один диалог на пару участников в контексте
    // карточки умершего (participant_a_id < participant_b_id).
    public const string UxRelativeConversationsPair =
        "ux_relative_conversations_deceased_a_b";
}