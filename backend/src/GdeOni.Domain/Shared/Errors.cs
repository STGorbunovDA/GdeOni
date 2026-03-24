namespace GdeOni.Domain.Shared;

public static class Errors
{
    public static class General
    {
        public static Error ValueIsInvalid(string? name = null)
        {
            var label = name ?? "value";
            return Error.Validation($"{label}.is.invalid", $"{label} is invalid");
        }

        public static Error ValueIsRequired(string? name = null)
        {
            var label = name ?? "value";
            return Error.Validation($"{label}.is.required", $"{label} is required");
        }

        public static Error NotFound(string entity = "record", Guid? id = null)
        {
            var forId = id == null ? string.Empty : $" for Id '{id}'";
            return Error.NotFound($"{entity}.not.found", $"{entity} not found{forId}");
        }

        public static Error AlreadyExists(string entity = "record")
        {
            return Error.Conflict($"{entity}.already.exists", $"{entity} already exists");
        }

        public static Error Conflict(string code, string message)
        {
            return Error.Conflict(code, message);
        }

        public static Error Failure(string code, string message)
        {
            return Error.Failure(code, message);
        }
    }

    public static class PersonName
    {
        public static Error FirstNameRequired() =>
            Error.Validation("person_name.first_name.required", "First name is required");

        public static Error LastNameRequired() =>
            Error.Validation("person_name.last_name.required", "Last name is required");
    }

    public static class LifePeriod
    {
        public static Error DeathDateRequired() =>
            Error.Validation("life_period.death_date.required", "Death date is required");

        public static Error DeathDateInFuture() =>
            Error.Validation("life_period.death_date.in_future", "Death date cannot be in the future");

        public static Error BirthDateAfterDeathDate() =>
            Error.Validation("life_period.birth_date.invalid", "Birth date cannot be later than death date");
    }

    public static class BurialLocation
    {
        public static Error LatitudeInvalid() =>
            Error.Validation("burial_location.latitude.invalid", "Latitude is invalid");

        public static Error LongitudeInvalid() =>
            Error.Validation("burial_location.longitude.invalid", "Longitude is invalid");

        public static Error CountryRequired() =>
            Error.Validation("burial_location.country.required", "Country is required");
    }

    public static class Deceased
    {
        public static Error CreatedByRequired() =>
            Error.Validation("deceased.created_by.required", "Created by user id is required");

        public static Error BurialLocationRequired() =>
            Error.Validation("deceased.burial_location.required", "Burial location is required");

        public static Error AlreadyVerified() =>
            Error.Conflict("deceased.already.verified", "Deceased record is already verified");

        public static Error NotVerified() =>
            Error.Conflict("deceased.not.verified", "Deceased record is not verified");

        public static Error MetadataRequired() =>
            Error.Validation("deceased.metadata.required", "Metadata is required");
        
        public static Error AlreadyExists() =>
            Error.Conflict("deceased.already.exists", "Such a deceased person already exists.");
    }

    public static class DeceasedPhoto
    {
        public static Error UrlRequired() =>
            Error.Validation("deceased_photo.url.required", "Photo url is required");

        public static Error AddedByRequired() =>
            Error.Validation("deceased_photo.added_by.required", "Added by user id is required");

        public static Error NotFound(Guid? id = null) =>
            Error.NotFound("deceased_photo.not.found", id == null
                ? "Photo not found"
                : $"Photo not found for Id '{id}'");

        public static Error AlreadyPrimary() =>
            Error.Conflict("deceased_photo.already.primary", "Photo is already primary");

        public static Error AlreadyApproved() =>
            Error.Conflict("deceased_photo.already.approved", "Photo is already approved");

        public static Error AlreadyRejected() =>
            Error.Conflict("deceased_photo.already.rejected", "Photo is already rejected");
    }

    public static class DeceasedMemory
    {
        public static Error TextRequired() =>
            Error.Validation("deceased_memory.text.required", "Memory text is required");

        public static Error NotFound(Guid? id = null) =>
            Error.NotFound("deceased_memory.not.found", id == null
                ? "Memory not found"
                : $"Memory not found for Id '{id}'");

        public static Error AlreadyApproved() =>
            Error.Conflict("deceased_memory.already.approved", "Memory is already approved");

        public static Error AlreadyRejected() =>
            Error.Conflict("deceased_memory.already.rejected", "Memory is already rejected");
    }

    public static class User
    {
        public static Error EmailRequired() =>
            Error.Validation("user.email.required", "Email is required");

        public static Error EmailInvalid() =>
            Error.Validation("user.email.invalid", "Email is invalid");

        public static Error PasswordHashRequired() =>
            Error.Validation("user.password_hash.required", "Password hash is required");
        
        public static Error PasswordRequired() =>
            Error.Validation("user.password.required", "Password is required");
        
        public static Error PasswordTooShort() =>
            Error.Validation("user.password.too_short", "Password too short");

        public static Error UserNameRequired() =>
            Error.Validation("user.user_name.required", "User name is required");

        public static Error EmailAlreadyExists() =>
            Error.Conflict("user.email.already.exists", "User with this email already exists");

        public static Error UserNameAlreadyExists() =>
            Error.Conflict("user.user_name.already.exists", "User with this user name already exists");
    }

    public static class Tracking
    {
        public static Error DeceasedIdRequired() =>
            Error.Validation("tracking.deceased_id.required", "Deceased id is required");

        public static Error AlreadyTracked() =>
            Error.Conflict("tracking.already.exists", "Deceased is already tracked by user");

        public static Error NotFound(Guid? deceasedId = null) =>
            Error.NotFound("tracking.not.found", deceasedId == null
                ? "Tracking not found"
                : $"Tracking not found for deceased Id '{deceasedId}'");

        public static Error AlreadyArchived() =>
            Error.Conflict("tracking.already.archived", "Tracking is already archived");

        public static Error AlreadyMuted() =>
            Error.Conflict("tracking.already.muted", "Tracking is already muted");

        public static Error AlreadyActive() =>
            Error.Conflict("tracking.already.active", "Tracking is already active");
    }
}