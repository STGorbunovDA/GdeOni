namespace GdeOni.Domain.Shared;

// Partial-split от Errors.cs. Subscription + Payment + Complimentary +
// Legal — связанный bounded context "монетизация".
public static partial class Errors
{
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

        /// <summary>
        /// Срабатывает по unique-индексу на external_payment_id —
        /// дубль webhook'а от YooKassa с тем же payment id (штатный
        /// retry-механизм платёжного провайдера).
        /// </summary>
        public static Error PaymentDuplicate() =>
            Error.Conflict(
                "subscription.payment.duplicate",
                "Payment with this external id has already been processed.");

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

        public static Error RevokeSelfForbidden() =>
            Error.Forbidden(
                "subscription.revoke.self_forbidden",
                "Admin cannot revoke their own subscription.");

        public static Error ManageSuperAdminForbidden() =>
            Error.Forbidden(
                "subscription.manage.super_admin_forbidden",
                "Admin cannot manage SuperAdmin's subscription.");
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
}
