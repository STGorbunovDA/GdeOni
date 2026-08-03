namespace GdeOni.Domain.Shared;

// Partial-split от Errors.cs. Функция «Родственники» — внутренняя переписка
// (turn-based чат: по одному сообщению по очереди).
public static partial class Errors
{
    public static class Relatives
    {
        public static Error MessageTextRequired() =>
            Error.Validation("relatives.message.text.required", "Message text is required");

        public static Error MessageTooLong(int max) =>
            Error.Validation("relatives.message.text.too_long", $"Message must be at most {max} characters");

        public static Error CannotMessageSelf() =>
            Error.Validation("relatives.conversation.self", "Cannot start a conversation with yourself");

        /// <summary>
        /// Собеседник не является родственником по этой карточке: нет общего
        /// активного отслеживания, связь не связывающая, согласие снято или
        /// пользователь заблокирован.
        /// </summary>
        public static Error CannotStartConversation() =>
            Error.Forbidden("relatives.conversation.not_allowed", "You cannot message this user");

        public static Error ConversationNotFound() =>
            Error.NotFound("relatives.conversation.not_found", "Conversation not found");

        public static Error MessageNotFound() =>
            Error.NotFound("relatives.message.not_found", "Message not found");

        /// <summary>Пользователь не участник диалога.</summary>
        public static Error NotParticipant() =>
            Error.Forbidden("relatives.conversation.forbidden", "You are not a participant of this conversation");

        /// <summary>Сообщение не принадлежит пользователю (правка/удаление чужого).</summary>
        public static Error NotOwnMessage() =>
            Error.Forbidden("relatives.message.forbidden", "You can only change your own message");

        /// <summary>
        /// Сейчас не ход пользователя: пока собеседник не ответит на предыдущее
        /// сообщение, отправить новое нельзя (переписка строго по очереди).
        /// </summary>
        public static Error NotYourTurn() =>
            Error.Conflict("relatives.message.not_your_turn", "Wait for the other person to reply before sending again");

        /// <summary>
        /// Сообщение уже нельзя изменить/удалить: собеседник ответил (или это
        /// не последнее сообщение).
        /// </summary>
        public static Error MessageLocked() =>
            Error.Conflict("relatives.message.locked", "This message can no longer be edited or deleted");

        // ─────────────── Жалобы (Фаза 5) ───────────────

        public static Error ReportReasonRequired() =>
            Error.Validation("relatives.report.reason.required", "Report reason is required");

        public static Error ReportReasonTooLong(int max) =>
            Error.Validation("relatives.report.reason.too_long", $"Report reason must be at most {max} characters");

        /// <summary>Нельзя пожаловаться на самого себя.</summary>
        public static Error CannotReportSelf() =>
            Error.Validation("relatives.report.self", "You cannot report yourself");

        public static Error ReportNotFound() =>
            Error.NotFound("relatives.report.not_found", "Report not found");
    }
}
