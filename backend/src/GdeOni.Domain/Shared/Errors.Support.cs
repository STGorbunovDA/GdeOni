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

        public static Error ViewForbidden() =>
            Error.Forbidden(
                "support_ticket.view.forbidden",
                "You can only view your own tickets.");
    }
}
