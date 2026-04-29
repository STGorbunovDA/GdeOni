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

        public static Error Unauthorized(string code = "auth.unauthorized", string message = "Authentication is required.")
        {
            return Error.Unauthorized(code, message);
        }

        public static Error Forbidden(string code, string message)
        {
            return Error.Forbidden(code, message);
        }
    }

    public static class PersonName
    {
        public static Error FirstNameRequired() =>
            Error.Validation("person_name.first_name.required", "First name is required");

        public static Error LastNameRequired() =>
            Error.Validation("person_name.last_name.required", "Last name is required");
        
        public static Error FirstNameTooLong(int maxLength) =>
            Error.Validation("person_name.first_name.too_long", $"First name must be at most {maxLength} characters");

        public static Error LastNameTooLong(int maxLength) =>
            Error.Validation("person_name.last_name.too_long", $"Last name must be at most {maxLength} characters");

        public static Error MiddleNameTooLong(int maxLength) =>
            Error.Validation("person_name.middle_name.too_long", $"Middle name must be at most {maxLength} characters");
        
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

        public static Error CountryTooLong(int maxLength) =>
            Error.Validation("burial_location.country.too_long", $"Country must be at most {maxLength} characters");

        public static Error AccuracyMetersInvalid() =>
            Error.Validation("burial_location.accuracy_meters.invalid", "Accuracy meters must be greater than or equal to zero");

        public static Error RegionTooLong(int maxLength) =>
            Error.Validation("burial_location.region.too_long", $"Region must be at most {maxLength} characters");

        public static Error CityTooLong(int maxLength) =>
            Error.Validation("burial_location.city.too_long", $"City must be at most {maxLength} characters");

        public static Error CemeteryNameTooLong(int maxLength) =>
            Error.Validation("burial_location.cemetery_name.too_long", $"Cemetery name must be at most {maxLength} characters");

        public static Error PlotNumberTooLong(int maxLength) =>
            Error.Validation("burial_location.plot_number.too_long", $"Plot number must be at most {maxLength} characters");

        public static Error GraveNumberTooLong(int maxLength) =>
            Error.Validation("burial_location.grave_number.too_long", $"Grave number must be at most {maxLength} characters");

        public static Error AccuracyInvalid() =>
            Error.Validation("burial_location.accuracy.invalid", "Burial location accuracy is invalid");
    }

    public static class Deceased
    {
        public static Error CreatedByRequired() =>
            Error.Validation("deceased.created_by.required", "Created by user id is required");

        public static Error IdRequired() =>
            Error.Validation("deceased.id.required", "Deceased id is required");

        public static Error BurialLocationNotSet() =>
            Error.Conflict("deceased.burial_location.not_set", "Burial location is not set for this deceased record");

        public static Error MetadataRequired() =>
            Error.Validation("deceased.metadata.required", "Metadata is required");

        public static Error AddMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.author.forbidden",
                "You cannot create a memory on behalf of another user.");
        
        public static Error UpdateMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.author.forbidden",
                "You cannot update a memory on behalf of another user.");
        
        public static Error DeleteMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.author.forbidden",
                "You cannot delete a memory on behalf of another user.");
        
        public static Error UpdateForbidden() =>
            Error.Forbidden(
                "deceased.update.forbidden",
                "You cannot update a deceased person's card on behalf of another user.");

        public static Error SetBurialLocationForbidden() =>
            Error.Forbidden(
                "deceased.burial_location.set.forbidden",
                "You cannot set the burial location on behalf of another user.");

        public static Error AlreadyVerified() =>
            Error.Conflict("deceased.already.verified", "Deceased record is already verified");

        public static Error NotVerified() =>
            Error.Conflict("deceased.not.verified", "Deceased record is not verified");

        public static Error AlreadyExists() =>
            Error.Conflict("deceased.already.exists", "Such a deceased person already exists.");

        public static Error ShortDescriptionTooLong(int maxLength) =>
            Error.Validation("deceased.short_description.too_long",
                $"Short description must be at most {maxLength} characters");

        public static Error BiographyTooLong(int maxLength) =>
            Error.Validation("deceased.biography.too_long",
                $"Biography must be at most {maxLength} characters");
        
        public static Error InsufficientPermissionsToViewAllDeceased() =>
            Error.Unauthorized("deceased.insufficient_permissions.view_all", 
                "You don't have permission to view all deceased. Admin or SuperAdmin rights are required.");

        public static Error EpitaphTooLong(int maxLength) =>
            Error.Validation("deceased.metadata.epitaph.too_long",
                $"Epitaph must be at most {maxLength} characters");

        public static Error ReligionTooLong(int maxLength) =>
            Error.Validation("deceased.metadata.religion.too_long",
                $"Religion must be at most {maxLength} characters");

        public static Error SourceTooLong(int maxLength) =>
            Error.Validation("deceased.metadata.source.too_long",
                $"Source must be at most {maxLength} characters");

        public static Error AdditionalInfoTooLong(int maxLength) =>
            Error.Validation("deceased.metadata.additional_info.too_long",
                $"Additional info must be at most {maxLength} characters");

        public static Error SearchTooLong(int maxLength) =>
            Error.Validation("deceased.search.too_long",
                $"Search must be at most {maxLength} characters");

        public static Error CreatedFromMustBeLessOrEqualToCreatedTo() =>
            Error.Validation("deceased.created_range.invalid",
                "CreatedFrom must be less than or equal to CreatedTo");

        public static Error CreatedFromInFuture() =>
            Error.Validation("deceased.created_from.in_future",
                "CreatedFrom cannot be in the future");

        public static Error CreatedToInFuture() =>
            Error.Validation("deceased.created_to.in_future",
                "CreatedTo cannot be in the future");
        
        public static Error DeleteForbidden() =>
            Error.Forbidden(
                "deceased.delete.forbidden",
                "You do not have permission to delete a deceased record.");
  
        public static Error VerifyForbidden() =>
            Error.Forbidden(
                "deceased.verify.forbidden",
                "You do not have permission to verify the deceased's account.");
        
        public static Error UnverifiedForbidden() =>
            Error.Forbidden(
                "deceased.unverified.forbidden",
                "You do not have permission to verify the deceased's account.");
    }
    
    public static class DeceasedPhoto
    {
        public static Error DuplicateUrl() =>
            Error.Conflict(
                "deceased_photo.url.duplicate",
                "A photo with the same URL already exists for this deceased card.");
        
        public static Error UrlRequired() =>
            Error.Validation("deceased_photo.url.required", "Photo url is required");
        
        public static Error AddPhotoForbidden() =>
            Error.Forbidden(
                "deceased_photo_add.author.forbidden",
                "You cannot added a photo on behalf of another user.");
        
        public static Error SetPrimaryPhotoForbidden() =>
            Error.Forbidden(
                "deceased_set_primary_photo.author.forbidden",
                "You cannot set primary photo on behalf of another user.");
        
        public static Error UpdatePhotoForbidden() =>
            Error.Forbidden(
                "deceased_update_photo.author.forbidden",
                "You cannot updated a photo on behalf of another user.");

        public static Error UrlInvalid() =>
            Error.Validation("deceased_photo.url.invalid", "Photo url invalid");

        public static Error AddedByRequired() =>
            Error.Validation("deceased_photo.added_by.required", "Added by user id is required");

        public static Error ApprovePhotoForbidden() =>
            Error.Forbidden(
                "deceased_photo_approve.verify.forbidden",
                "You do not have the right to verify the photo of the deceased");
        
        public static Error RejectPhotoForbidden() =>
            Error.Forbidden(
                "deceased_photo_reject.verify.forbidden",
                "You do not have permission to reject the confirmation of the deceased's photo");

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

        public static Error UrlTooLong(int maxLength) =>
            Error.Validation("deceased_photo.url.too_long", $"Photo url must be at most {maxLength} characters");

        public static Error DeletePhotoForbidden() =>
            Error.Forbidden(
                "deceased_photo.author.forbidden",
                "You cannot delete a photo on behalf of another user.");
        
        public static Error DescriptionTooLong(int maxLength) =>
            Error.Validation("deceased_photo.description.too_long", $"Photo description must be at most {maxLength} characters");
    }
    
    public static class DeceasedMetadata
    {
        public static Error UpdateDeceasedMetadataForbidden() =>
            Error.Forbidden(
                "deceased_metadata.update.forbidden",
                "You cannot update a deceased person's metadata card on behalf of another user.");
        
        public static Error DeleteDeceasedMetadataForbidden() =>
            Error.Forbidden(
                "deceased_metadata.author.forbidden",
                "You cannot delete a deceased person's metadata card on behalf of another user.");
    }

    public static class DeceasedMemory
    {
        public static Error TextRequired() =>
            Error.Validation("deceased_memory.text.required", "Memory text is required");

        public static Error ApproveMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory_approve.verify.forbidden",
                "You have no right to verify the authenticity of a deceased person's recording.");
        
        public static Error RejectMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory_reject.verify.forbidden",
                "You have no right to verify the authenticity of a deceased person's recording.");

        public static Error NotFound(Guid? id = null) =>
            Error.NotFound("deceased_memory.not.found", id == null
                ? "Memory not found"
                : $"Memory not found for Id '{id}'");

        public static Error AlreadyApproved() =>
            Error.Conflict("deceased_memory.already.approved", "Memory is already approved");

        public static Error AlreadyRejected() =>
            Error.Conflict("deceased_memory.already.rejected", "Memory is already rejected");

        public static Error TextTooLong(int maxLength) =>
            Error.Validation("deceased_memory.text.too_long", $"Memory text must be at most {maxLength} characters");
    }

    public static class User
    {
        public static Error IdRequired() =>
            Error.Validation("user.id.required", "User id is required");

        public static Error EmailRequired() =>
            Error.Validation("user.email.required", "Email is required");

        public static Error EmailInvalid() =>
            Error.Validation("user.email.invalid", "Email is invalid");

        public static Error EmailTooLong(int maxLength) =>
            Error.Validation("user.email.too_long", $"Email must be at most {maxLength} characters");

        public static Error UserNameRequired() =>
            Error.Validation("user.user_name.required", "User name is required");

        public static Error UserNameTooLong(int maxLength) =>
            Error.Validation("user.user_name.too_long", $"User name must be at most {maxLength} characters");

        public static Error FullNameTooLong(int maxLength) =>
            Error.Validation("user.full_name.too_long", $"Full name must be at most {maxLength} characters");

        public static Error PasswordHashRequired() =>
            Error.Validation("user.password_hash.required", "Password hash is required");

        public static Error PasswordRequired() =>
            Error.Validation("user.password.required", "Password is required");

        public static Error PasswordTooShort(int minLength) =>
            Error.Validation("user.password.too_short", $"Password must be at least {minLength} characters long");

        public static Error EmailAlreadyExists() =>
            Error.Conflict("user.email.already.exists", "User with this email already exists");

        public static Error UserNameAlreadyExists() =>
            Error.Conflict("user.user_name.already.exists", "User with this user name already exists");

        public static Error InvalidCredentials() =>
            Error.Unauthorized("user.invalid.credentials", "Invalid email or password");

        public static Error CurrentPasswordInvalid() =>
            Error.Unauthorized("user.current_password.invalid", "Current password is invalid");

        public static Error RoleInvalid() =>
            Error.Validation("user.role.invalid", "User role is invalid");
        
        public static Error UserForbidden() =>
            Error.Validation("user.forbidden", "You do not have permission to access the current user.");

        public static Error RoleUnknownNotAllowed() =>
            Error.Validation("user.role.unknown.not_allowed", "The role cannot be Unknown");

        public static Error RoleSuperAdminNotAllowed() =>
            Error.Validation("user.role.super_admin.not_allowed", "The SuperAdmin role cannot be assigned");

        public static Error SearchTooLong(int maxLength) =>
            Error.Validation("user.search.too_long", $"Search must be at most {maxLength} characters");

        public static Error RegisteredAtUtcInFuture() =>
            Error.Validation("user.registered_at_utc.in_future", "RegisteredAtUtc cannot be in the future");

        public static Error LastLoginAtUtcInFuture() =>
            Error.Validation("user.last_login_at_utc.in_future", "LastLoginAtUtc cannot be in the future");
        
        public static Error InsufficientPermissionsToViewAllUsers() =>
            Error.Unauthorized("user.insufficient_permissions.view_all", 
                "You don't have permission to view all users. Admin or SuperAdmin rights are required.");
    }

    public static class Tracking
    {
        public static Error DeceasedIdRequired() =>
            Error.Validation("tracking.deceased_id.required", "Deceased id is required");

        public static Error DtoRequired() =>
            Error.Validation("tracking.dto.required", "Request body is required");

        public static Error RelationshipTypeInvalid() =>
            Error.Validation("tracking.relationship_type.invalid", "Relationship type is invalid");
        
        public static Error TrackStatusTypeInvalid() =>
            Error.Validation("tracking.track_status.invalid", "TrackStatus type is invalid");

        public static Error PersonalNotesTooLong(int maxLength) =>
            Error.Validation("tracking.personal_notes.too_long", $"Personal notes must be at most {maxLength} characters");

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

    public static class Pagination
    {
        public static Error PageMustBeGreaterThanZero() =>
            Error.Validation("pagination.page.invalid", "Page must be greater than 0");

        public static Error PageSizeOutOfRange(int min, int max) =>
            Error.Validation("pagination.page_size.invalid", $"PageSize must be between {min} and {max}");
    }

    public static class UniqueConstraint
    {
        public static Error FromName(string? constraintName) =>
            constraintName switch
            {
                DbConstraints.UxUsersEmail => User.EmailAlreadyExists(),
                DbConstraints.UxUsersName => User.UserNameAlreadyExists(),
                DbConstraints.DeceasedSearchKey => Deceased.AlreadyExists(),
                _ => Error.Conflict(
                    "conflict.unique_constraint",
                    "A unique constraint was violated.")
            };
    }
}