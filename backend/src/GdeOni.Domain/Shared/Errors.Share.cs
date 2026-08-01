namespace GdeOni.Domain.Shared;

// Partial-split от Errors.cs. D46 — «Поделиться подборкой карточек».
public static partial class Errors
{
    public static class Share
    {
        public static Error CodeRequired() =>
            Error.Validation("share.code.required", "Share code is required");

        public static Error CreatedByRequired() =>
            Error.Validation("share.created_by.required", "CreatedBy user id is required");

        public static Error DeceasedIdsRequired() =>
            Error.Validation("share.deceased_ids.required", "At least one deceased must be selected to share");

        public static Error TooManyItems(int max) =>
            Error.Validation("share.deceased_ids.too_many", $"You can share at most {max} cards at once");

        public static Error LifetimeInvalid() =>
            Error.Validation("share.lifetime.invalid", "Share link lifetime must be positive");

        /// <summary>
        /// D46. Подборка по коду не найдена. Тем же кодом отвечаем и на
        /// истёкшую, если хотим не палить существование — но здесь получатель
        /// уже вошёл, скрывать нечего: см. отдельный <see cref="Expired"/>.
        /// </summary>
        public static Error NotFound() =>
            Error.NotFound("share.bundle.not_found", "Share link is invalid or no longer exists");

        public static Error Expired() =>
            Error.NotFound("share.bundle.expired", "Share link has expired");

        /// <summary>
        /// D46. Не удалось подобрать уникальный код за отведённое число
        /// попыток (крайне маловероятно) — просим повторить.
        /// </summary>
        public static Error CodeGenerationFailed() =>
            Error.Failure("share.code.generation_failed", "Could not generate a unique share link, please try again");
    }
}
