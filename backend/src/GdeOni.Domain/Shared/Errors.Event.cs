namespace GdeOni.Domain.Shared;

// Partial-split от Errors.cs. Ручные (пользовательские) события в «Событиях».
public static partial class Errors
{
    public static class Event
    {
        public static Error TitleRequired() =>
            Error.Validation("event.title.required", "Event title is required");

        public static Error TitleTooLong(int max) =>
            Error.Validation("event.title.too_long", $"Event title must be at most {max} characters");

        public static Error NotFound() =>
            Error.NotFound("event.not_found", "Event not found");
    }
}
