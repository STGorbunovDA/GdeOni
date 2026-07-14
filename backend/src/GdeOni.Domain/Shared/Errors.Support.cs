namespace GdeOni.Domain.Shared;

public static partial class Errors
{
    public static class Support
    {
        public static Error TitleRequired() =>
            Error.Validation(
                "support_ticket.title.required",
                "Title is required.");

        public static Error TitleTooLong(int max) =>
            Error.Validation(
                "support_ticket.title.too_long",
                $"Title must not exceed {max} characters.");

        public static Error DescriptionRequired() =>
            Error.Validation(
                "support_ticket.description.required",
                "Description is required.");

        public static Error DescriptionTooLong(int max) =>
            Error.Validation(
                "support_ticket.description.too_long",
                $"Description must not exceed {max} characters.");

        public static Error KindInvalid() =>
            Error.Validation(
                "support_ticket.kind.invalid",
                "Ticket kind is invalid.");

        public static Error SeverityInvalid() =>
            Error.Validation(
                "support_ticket.severity.invalid",
                "Ticket severity is invalid.");

        public static Error StatusInvalid() =>
            Error.Validation(
                "support_ticket.status.invalid",
                "Ticket status is invalid.");

        public static Error ResolutionNoteRequired() =>
            Error.Validation(
                "support_ticket.resolution_note.required",
                "Resolution note is required when resolving a ticket.");

        public static Error ResolutionNoteTooLong(int max) =>
            Error.Validation(
                "support_ticket.resolution_note.too_long",
                $"Resolution note must not exceed {max} characters.");

        public static Error AlreadyResolved() =>
            Error.Conflict(
                "support_ticket.already.resolved",
                "Ticket is already resolved.");

        /// <summary>
        /// D40. Тикет закрыт принудительно — это терминальное состояние:
        /// ни статус, ни приоритет менять уже нельзя, переоткрыть тоже.
        /// </summary>
        public static Error AlreadyClosed() =>
            Error.Conflict(
                "support_ticket.already.closed",
                "Ticket is already closed.");

        public static Error ViewForbidden() =>
            Error.Forbidden(
                "support_ticket.view.forbidden",
                "You can only view your own tickets.");

        public static Error AcceptOnlyAfterResolved() =>
            Error.Conflict(
                "support_ticket.accept.only_after_resolved",
                "You can accept resolution only after the ticket is resolved.");

        public static Error AlreadyAccepted() =>
            Error.Conflict(
                "support_ticket.already.accepted",
                "You have already accepted the resolution.");

        public static Error ReopenOnlyAfterResolved() =>
            Error.Conflict(
                "support_ticket.reopen.only_after_resolved",
                "You can reopen the ticket only after it is resolved.");

        public static Error UserReplyTooLong(int max) =>
            Error.Validation(
                "support_ticket.user_reply.too_long",
                $"Your reply must not exceed {max} characters.");

        public static Error ModifyForbidden() =>
            Error.Forbidden(
                "support_ticket.modify.forbidden",
                "You can only accept or reopen your own tickets.");

        public static Error MessageTextRequired() =>
            Error.Validation(
                "support_ticket.message.text.required",
                "Message text is required.");

        public static Error MessageTextTooLong(int max) =>
            Error.Validation(
                "support_ticket.message.text.too_long",
                $"Message text must not exceed {max} characters.");

        // D33. Вложения в тикет.

        public static Error AttachmentsLimitExceeded(int max) =>
            Error.Validation(
                "support_ticket.attachments.limit_exceeded",
                $"Ticket cannot have more than {max} attachments.");

        public static Error AttachmentsTotalSizeExceeded(long maxBytes) =>
            Error.Validation(
                "support_ticket.attachments.total_size_exceeded",
                $"Total attachments size must not exceed {maxBytes} bytes.");

        public static Error AttachmentFileNameRequired() =>
            Error.Validation(
                "support_ticket.attachment.file_name.required",
                "Attachment file name is required.");

        public static Error AttachmentFileNameTooLong(int max) =>
            Error.Validation(
                "support_ticket.attachment.file_name.too_long",
                $"Attachment file name must not exceed {max} characters.");

        public static Error AttachmentContentTypeRequired() =>
            Error.Validation(
                "support_ticket.attachment.content_type.required",
                "Attachment content type is required.");

        public static Error AttachmentSizeInvalid() =>
            Error.Validation(
                "support_ticket.attachment.size.invalid",
                "Attachment size must be greater than zero.");

        public static Error AttachmentNotFound() =>
            Error.NotFound(
                "support_ticket.attachment.not_found",
                "Attachment was not found.");
    }
}
