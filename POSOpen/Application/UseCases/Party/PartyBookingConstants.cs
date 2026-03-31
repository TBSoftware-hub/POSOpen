namespace POSOpen.Application.UseCases.Party;

public static class PartyBookingConstants
{
	public static readonly string[] KnownSlotIds = ["10:00", "13:00", "16:00"];

	public const string ErrorBookingNotFound = "BOOKING_NOT_FOUND";
	public const string ErrorDateInvalid = "BOOKING_DATE_INVALID";
	public const string ErrorSlotRequired = "BOOKING_SLOT_REQUIRED";
	public const string ErrorSlotInvalid = "BOOKING_SLOT_INVALID";
	public const string ErrorSlotUnavailable = "BOOKING_SLOT_UNAVAILABLE";
	public const string ErrorPackageRequired = "BOOKING_PACKAGE_REQUIRED";
	public const string ErrorStateInvalid = "BOOKING_STATE_INVALID";
	public const string ErrorCommitFailed = "BOOKING_COMMIT_FAILED";

	public const string SafeBookingNotFoundMessage = "The selected booking could not be found.";
	public const string SafeDateInvalidMessage = "Choose a future party date to continue.";
	public const string SafeSlotRequiredMessage = "Choose an available time slot to continue.";
	public const string SafeSlotInvalidMessage = "The selected time slot is not valid.";
	public const string SafeSlotUnavailableMessage = "That time slot is no longer available.";
	public const string SafePackageRequiredMessage = "Choose a package to continue.";
	public const string SafeStateInvalidMessage = "This booking cannot be confirmed in its current state.";
	public const string SafeCommitFailedMessage = "Booking save failed. Please try again.";

	public const string DraftSavedMessage = "Draft booking saved.";
	public const string DraftAlreadySavedMessage = "Draft booking already saved for this operation.";
	public const string BookingConfirmedMessage = "Booking confirmed successfully.";
	public const string BookingAlreadyConfirmedMessage = "Booking was already confirmed.";

	public const string WizardInitialMessage = "Select booking details to begin.";
	public const string WizardSelectTimeMessage = "Choose an available time slot.";
	public const string WizardSelectPackageMessage = "Choose a package.";
	public const string AvailabilityLoadedMessage = "Availability loaded.";
}
