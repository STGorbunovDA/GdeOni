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

        public static Error InternalServerError() =>
            Error.Failure("server.internal", "An unexpected server error occurred.");

        public static Error TooManyRequests() =>
            Error.TooManyRequests(
                "general.too_many_requests",
                "Too many requests. Please slow down and try again later.");
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
            Error.Validation("life_period.birth_date.after_death_date", "Birth date cannot be later than death date");
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

        public static Error UpdateMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.update.forbidden",
                "You cannot update a memory on behalf of another user.");

        public static Error DeleteMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.delete.forbidden",
                "You cannot delete a memory on behalf of another user.");
        
        public static Error UpdateForbidden() =>
            Error.Forbidden(
                "deceased.update.forbidden",
                "You cannot update a deceased person's card on behalf of another user.");

        public static Error SetBurialLocationForbidden() =>
            Error.Forbidden(
                "deceased.burial_location.set.forbidden",
                "You cannot set the burial location on behalf of another user.");

        public static Error ClearBurialLocationForbidden() =>
            Error.Forbidden(
                "deceased.burial_location.clear.forbidden",
                "You cannot clear the burial location on behalf of another user.");

        public static Error BurialLocationAlreadyNull() =>
            Error.Conflict(
                "deceased.burial_location.already_null",
                "Burial location is already null and cannot be cleared again.");

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
            Error.Forbidden("deceased.insufficient_permissions.view_all",
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
                "deceased.unverify.forbidden",
                "You do not have permission to unverify the deceased's account.");
    }
    
    public static class DeceasedMetadata
    {
        public static Error UpdateDeceasedMetadataForbidden() =>
            Error.Forbidden(
                "deceased_metadata.update.forbidden",
                "You cannot update a deceased person's metadata card on behalf of another user.");
        
        public static Error DeleteDeceasedMetadataForbidden() =>
            Error.Forbidden(
                "deceased_metadata.delete.forbidden",
                "You cannot delete a deceased person's metadata card on behalf of another user.");
    }

    public static class DeceasedMemory
    {
        public static Error TextRequired() =>
            Error.Validation("deceased_memory.text.required", "Memory text is required");

        public static Error ApproveMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.approve.forbidden",
                "You have no right to verify the authenticity of a deceased person's recording.");

        public static Error RejectMemoryForbidden() =>
            Error.Forbidden(
                "deceased_memory.reject.forbidden",
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

        public static Error PasswordTooLong(int maxLength) =>
            Error.Validation("user.password.too_long", $"Password must be at most {maxLength} characters long");

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
            Error.Forbidden("user.forbidden", "You do not have permission to access the current user.");

        public static Error ChangeSuperAdminRoleForbidden() =>
            Error.Forbidden(
                "user.role.change.super_admin.forbidden",
                "Only a SuperAdmin can change the role of another SuperAdmin.");

        public static Error ChangePeerAdminRoleForbidden() =>
            Error.Forbidden(
                "user.role.change.peer_admin.forbidden",
                "An Admin cannot change the role of another Admin. Only a SuperAdmin can.");

        public static Error DeleteSuperAdminForbidden() =>
            Error.Forbidden(
                "user.delete.super_admin.forbidden",
                "SuperAdmin cannot be deleted.");

        public static Error DeleteSelfForbidden() =>
            Error.Forbidden(
                "user.delete.self.forbidden",
                "You cannot delete your own account.");

        public static Error DeletePeerAdminForbidden() =>
            Error.Forbidden(
                "user.delete.peer_admin.forbidden",
                "An Admin cannot delete another Admin. Only a SuperAdmin can.");

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
            Error.Forbidden("user.insufficient_permissions.view_all",
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

        public static Error NotTracked() =>
            Error.NotFound(
                "tracking.not_tracked",
                "Current user does not track this deceased.");
    }

    public static class Routing
    {
        public static Error RoutingModeInvalid() =>
            Error.Validation("routing.mode.invalid", "Routing mode is invalid.");
    }

    public static class Pagination
    {
        public static Error PageMustBeGreaterThanZero() =>
            Error.Validation("pagination.page.invalid", "Page must be greater than 0");

        public static Error PageSizeOutOfRange(int min, int max) =>
            Error.Validation("pagination.page_size.invalid", $"PageSize must be between {min} and {max}");
    }

    public static class NearbySearch
    {
        public static Error RadiusOutOfRange(int min, int max) =>
            Error.Validation("nearby_search.radius.invalid", $"RadiusMeters must be between {min} and {max}");
    }

    public static class Subscription
    {
        public static Error SubscriptionRequired() =>
            Error.Forbidden(
                "subscription.required",
                "Active subscription is required to access this resource.");

        public static Error AlreadyActive() =>
            Error.Conflict(
                "subscription.already.active",
                "Subscription is already active. Wait until the current period ends or cancel it first.");

        public static Error PaymentNotFound() =>
            Error.NotFound(
                "subscription.payment.not_found",
                "Payment was not found for any user.");

        public static Error InvalidPaymentSignature() =>
            Error.Unauthorized(
                "subscription.payment.invalid_signature",
                "Payment webhook signature is invalid.");

        public static Error PlanInvalid() =>
            Error.Validation(
                "subscription.plan.invalid",
                "Subscription plan is invalid.");

        public static Error PaymentIdRequired() =>
            Error.Validation(
                "subscription.payment_id.required",
                "Payment id is required.");

        public static Error PaymentIdTooLong(int maxLength) =>
            Error.Validation(
                "subscription.payment_id.too_long",
                $"Payment id must be at most {maxLength} characters.");

        public static Error ExpiresAtInPast() =>
            Error.Validation(
                "subscription.expires_at.in_past",
                "Subscription ExpiresAtUtc must be in the future.");

        public static Error TrialDurationInvalid() =>
            Error.Validation(
                "subscription.trial_duration.invalid",
                "Trial duration must be positive.");

        public static Error NotCancellable() =>
            Error.Conflict(
                "subscription.not_cancellable",
                "Subscription cannot be cancelled from the current state.");
    }

    public static class Legal
    {
        public static Error PrivacyPolicyNotAccepted() =>
            Error.Validation(
                "legal.privacy_policy.not_accepted",
                "You must accept the Privacy Policy to register.");

        public static Error TermsNotAccepted() =>
            Error.Validation(
                "legal.terms.not_accepted",
                "You must accept the Terms of Use to register.");

        public static Error PrivacyPolicyVersionInvalid() =>
            Error.Validation(
                "legal.privacy_policy.version.invalid",
                "Privacy Policy version must be a positive integer.");

        public static Error TermsVersionInvalid() =>
            Error.Validation(
                "legal.terms.version.invalid",
                "Terms of Use version must be a positive integer.");

        public static Error DocumentNotFound(string documentKey) =>
            Error.NotFound(
                "legal.document.not_found",
                $"Legal document '{documentKey}' was not found on the server.");

        public static Error VersionOutdated() =>
            Error.Conflict(
                "legal.version.outdated",
                "The submitted Privacy Policy or Terms version is older than the current one. Reload the documents and accept the latest versions.");
    }

    public static class Payment
    {
        public static Error ExternalPaymentIdRequired() =>
            Error.Validation(
                "payment.external_id.required",
                "External payment id is required.");

        public static Error ExternalPaymentIdTooLong(int maxLength) =>
            Error.Validation(
                "payment.external_id.too_long",
                $"External payment id must be at most {maxLength} characters.");

        public static Error CheckoutUrlTooLong(int maxLength) =>
            Error.Validation(
                "payment.checkout_url.too_long",
                $"Checkout URL must be at most {maxLength} characters.");

        public static Error AmountInvalid() =>
            Error.Validation(
                "payment.amount.invalid",
                "Payment amount must be a positive number.");

        public static Error AlreadyProcessed() =>
            Error.Conflict(
                "payment.already_processed",
                "Payment is already in a final state and cannot be modified.");

        public static Error PeriodInvalid() =>
            Error.Validation(
                "payment.period.invalid",
                "Payment period end must be later than period start.");

        public static Error NotFound(string externalPaymentId) =>
            Error.NotFound(
                "payment.not_found",
                $"Payment with external id '{externalPaymentId}' was not found.");
    }

    public static class Complimentary
    {
        public static Error AdminIdRequired() =>
            Error.Validation(
                "complimentary.admin_id.required",
                "AdminId is required to grant complimentary access.");

        public static Error UntilDateInPast() =>
            Error.Validation(
                "complimentary.until.in_past",
                "Complimentary access UntilUtc must be in the future or null for unlimited access.");

        public static Error NoteTooLong(int maxLength) =>
            Error.Validation(
                "complimentary.note.too_long",
                $"Complimentary access note must be at most {maxLength} characters.");

        public static Error GrantToSelfForbidden() =>
            Error.Forbidden(
                "complimentary.grant.self.forbidden",
                "An administrator cannot grant complimentary access to themselves.");

        public static Error ManageSuperAdminForbidden() =>
            Error.Forbidden(
                "complimentary.manage.super_admin.forbidden",
                "Only a SuperAdmin can grant or revoke complimentary access for another SuperAdmin.");
    }

    public static class UniqueConstraint
    {
        public static Error FromName(string? constraintName) =>
            constraintName switch
            {
                DbConstraints.UxUsersEmail => User.EmailAlreadyExists(),
                DbConstraints.UxUsersName => User.UserNameAlreadyExists(),
                DbConstraints.DeceasedSearchKey => Deceased.AlreadyExists(),
                DbConstraints.UxDeceasedMediaStorageKey => DeceasedMedia.DuplicateStorageKey(),
                DbConstraints.UxRefreshTokensTokenHash => RefreshToken.TokenHashAlreadyExists(),
                _ => Error.Conflict(
                    "conflict.unique_constraint",
                    "A unique constraint was violated.")
            };
    }

    public static class DeceasedMedia
    {
        public static Error NotFound(Guid mediaId) =>
            Error.NotFound("deceased_media.not.found", $"Deceased media not found for Id '{mediaId}'");

        public static Error IdRequired() =>
            Error.Validation("deceased_media.id.required", "Media id is required");

        public static Error DeceasedIdRequired() =>
            Error.Validation("deceased_media.deceased_id.required", "DeceasedId is required");

        public static Error UploadedByRequired() =>
            Error.Validation("deceased_media.uploaded_by.required", "UploadedByUserId is required");

        public static Error KindInvalid() =>
            Error.Validation("deceased_media.kind.invalid", "Media kind is invalid");

        public static Error SizeBytesInvalid() =>
            Error.Validation("deceased_media.size_bytes.invalid", "SizeBytes must be greater than 0");

        public static Error OriginalFileNameRequired() =>
            Error.Validation("deceased_media.original_file_name.required", "Original file name is required");

        public static Error OriginalFileNameTooLong(int maxLength) =>
            Error.Validation("deceased_media.original_file_name.too_long", $"Original file name must be at most {maxLength} characters");

        public static Error BucketRequired() =>
            Error.Validation("deceased_media.bucket.required", "Bucket is required");

        public static Error BucketTooLong(int maxLength) =>
            Error.Validation("deceased_media.bucket.too_long", $"Bucket must be at most {maxLength} characters");

        public static Error StorageKeyRequired() =>
            Error.Validation("deceased_media.storage_key.required", "Storage key is required");

        public static Error StorageKeyTooLong(int maxLength) =>
            Error.Validation("deceased_media.storage_key.too_long", $"Storage key must be at most {maxLength} characters");

        public static Error ContentTypeRequired() =>
            Error.Validation("deceased_media.content_type.required", "Content type is required");

        public static Error ContentTypeTooLong(int maxLength) =>
            Error.Validation("deceased_media.content_type.too_long", $"Content type must be at most {maxLength} characters");

        public static Error DescriptionTooLong(int maxLength) =>
            Error.Validation("deceased_media.description.too_long", $"Description must be at most {maxLength} characters");

        public static Error OnlyDeceasedPhotoCanBeMain() =>
            Error.Conflict("deceased_media.main_photo.only_deceased_photo", "Only DeceasedPhoto can be main photo");

        public static Error MainPhotoMustBeApproved() =>
            Error.Conflict(
                "deceased_media.main_photo.not_approved",
                "Only an Approved photo can be set as main. Wait for moderation, then try again.");

        public static Error AlreadyApproved() =>
            Error.Conflict("deceased_media.already.approved", "Media is already approved");

        public static Error AlreadyRejected() =>
            Error.Conflict("deceased_media.already.rejected", "Media is already rejected");

        public static Error DuplicateStorageKey() =>
            Error.Conflict("deceased_media.storage_key.duplicate", "Media with such storage key already exists");

        public static Error UploadForbidden() =>
            Error.Forbidden(
                "deceased_media.upload.forbidden",
                "You don't have permission to upload media for this deceased.");

        public static Error DeleteForbidden() =>
            Error.Forbidden(
                "deceased_media.delete.forbidden",
                "You don't have permission to delete this media.");

        public static Error UpdateDescriptionForbidden() =>
            Error.Forbidden(
                "deceased_media.update_description.forbidden",
                "You don't have permission to update description for this media.");

        public static Error SetMainPhotoForbidden() =>
            Error.Forbidden(
                "deceased_media.main_photo.forbidden",
                "Only the deceased card author or admin can set the main photo.");

        public static Error ModerationForbidden() =>
            Error.Forbidden(
                "deceased_media.moderation.forbidden",
                "Only Admin or SuperAdmin can moderate media.");
    }

    public static class Media
    {
        public static Error PhotoContentTypeNotAllowed(string contentType) =>
            Error.Validation(
                "media.photo.content_type.not_allowed",
                $"Content type '{contentType}' is not allowed for photos. Allowed: image/jpeg, image/png, image/webp.");

        public static Error DocumentContentTypeNotAllowed(string contentType) =>
            Error.Validation(
                "media.document.content_type.not_allowed",
                $"Content type '{contentType}' is not allowed for documents. Allowed: application/pdf.");

        public static Error PhotoTooLarge(long maxBytes) =>
            Error.Validation(
                "media.photo.too_large",
                $"Photo size exceeds {maxBytes} bytes.");

        public static Error DocumentTooLarge(long maxBytes) =>
            Error.Validation(
                "media.document.too_large",
                $"Document size exceeds {maxBytes} bytes.");

        public static Error FileRequired() =>
            Error.Validation("media.file.required", "File is required");

        public static Error MagicBytesMismatch(string contentType) =>
            Error.Validation(
                "media.content.magic_bytes_mismatch",
                $"File content does not match declared content type '{contentType}'.");

        public static Error UnreadableStream() =>
            Error.Validation(
                "media.content.unreadable",
                "File stream cannot be read or sought.");
    }

    public static class RefreshToken
    {
        public static Error TokenRequired() =>
            Error.Validation("refresh_token.token.required", "Refresh token is required");

        public static Error TokenHashRequired() =>
            Error.Validation("refresh_token.token_hash.required", "Refresh token hash is required");

        public static Error TokenExpiresInPast() =>
            Error.Validation("refresh_token.expires_at.invalid", "Refresh token expiration must be in the future");

        public static Error IpTooLong(int maxLength) =>
            Error.Validation("refresh_token.ip.too_long", $"Created from IP must be at most {maxLength} characters");

        public static Error TokenInvalid() =>
            Error.Unauthorized("refresh_token.invalid", "Refresh token is invalid");

        public static Error TokenExpired() =>
            Error.Unauthorized("refresh_token.expired", "Refresh token has expired");

        public static Error TokenRevoked() =>
            Error.Unauthorized("refresh_token.revoked", "Refresh token has been revoked");

        public static Error TokenAlreadyRevoked() =>
            Error.Conflict("refresh_token.already_revoked", "Refresh token has already been revoked");

        public static Error ReplayDetected() =>
            Error.Unauthorized(
                "refresh_token.replay_detected",
                "Refresh token replay detected. All active sessions have been revoked.");

        public static Error TokenHashAlreadyExists() =>
            Error.Conflict(
                "refresh_token.token_hash.duplicate",
                "Refresh token hash collision detected.");
    }
}