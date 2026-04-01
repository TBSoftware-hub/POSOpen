namespace POSOpen.Application.UseCases.Party;

public static class PartyBookingConstants
{
	public static readonly string[] KnownSlotIds = ["10:00", "13:00", "16:00"];
	public static readonly string[] KnownRoomIds = ["room-a", "room-b", "room-c"];

	public const string ErrorBookingNotFound = "BOOKING_NOT_FOUND";
	public const string ErrorDateInvalid = "BOOKING_DATE_INVALID";
	public const string ErrorSlotRequired = "BOOKING_SLOT_REQUIRED";
	public const string ErrorSlotInvalid = "BOOKING_SLOT_INVALID";
	public const string ErrorSlotUnavailable = "BOOKING_SLOT_UNAVAILABLE";
	public const string ErrorPackageRequired = "BOOKING_PACKAGE_REQUIRED";
	public const string ErrorStateInvalid = "BOOKING_STATE_INVALID";
	public const string ErrorCommitFailed = "BOOKING_COMMIT_FAILED";
	public const string ErrorDepositAmountInvalid = "BOOKING_DEPOSIT_AMOUNT_INVALID";
	public const string ErrorDepositCurrencyInvalid = "BOOKING_DEPOSIT_CURRENCY_INVALID";
	public const string ErrorTimelineUnavailable = "BOOKING_TIMELINE_UNAVAILABLE";

	public const string SafeBookingNotFoundMessage = "The selected booking could not be found.";
	public const string SafeDateInvalidMessage = "Choose a future party date to continue.";
	public const string SafeSlotRequiredMessage = "Choose an available time slot to continue.";
	public const string SafeSlotInvalidMessage = "The selected time slot is not valid.";
	public const string SafeSlotUnavailableMessage = "That time slot is no longer available.";
	public const string SafePackageRequiredMessage = "Choose a package to continue.";
	public const string SafeStateInvalidMessage = "This booking cannot be confirmed in its current state.";
	public const string SafeCommitFailedMessage = "Booking save failed. Please try again.";
	public const string SafeDepositAmountInvalidMessage = "Enter a valid deposit amount to continue.";
	public const string SafeDepositCurrencyInvalidMessage = "Enter a valid 3-letter currency code to continue.";
	public const string SafeTimelineUnavailableMessage = "Timeline data is unavailable for this booking.";

	public const string ErrorRoomConflict = "BOOKING_ROOM_CONFLICT";
	public const string ErrorRoomInvalid = "BOOKING_ROOM_INVALID";
	public const string SafeRoomConflictMessage = "That room is already booked. Choose an alternative below.";
	public const string RoomAssignedMessage = "Room assignment saved.";
	public const string SafeRoomAssignmentFailedMessage = "Room assignment failed. Please try again.";

	public const string DraftSavedMessage = "Draft booking saved.";
	public const string DraftAlreadySavedMessage = "Draft booking already saved for this operation.";
	public const string BookingConfirmedMessage = "Booking confirmed successfully.";
	public const string BookingAlreadyConfirmedMessage = "Booking was already confirmed.";
	public const string DepositCommittedMessage = "Deposit commitment saved.";
	public const string DepositAlreadyCommittedMessage = "Deposit commitment was already recorded.";
	public const string BookingMarkedCompletedMessage = "Booking marked as completed.";
	public const string BookingAlreadyCompletedMessage = "Booking is already completed.";
	public const string TimelineLoadedMessage = "Booking timeline loaded.";

	public const string NextActionCaptureDepositCode = "capture-deposit";
	public const string NextActionPrepareArrivalCode = "prepare-arrival";
	public const string NextActionMonitorActiveCode = "monitor-active";
	public const string NextActionMarkCompletedCode = "mark-completed";
	public const string NextActionClosedCode = "closed";

	public const string NextActionCaptureDepositLabel = "Capture deposit commitment.";
	public const string NextActionPrepareArrivalLabel = "Prepare team and confirm party arrival.";
	public const string NextActionMonitorActiveLabel = "Monitor active party progress and exceptions.";
	public const string NextActionMarkCompletedLabel = "Mark booking complete when event closes.";
	public const string NextActionClosedLabel = "No further action required.";

	public const string WizardInitialMessage = "Select booking details to begin.";
	public const string WizardSelectTimeMessage = "Choose an available time slot.";
	public const string WizardSelectPackageMessage = "Choose a package.";
	public const string AvailabilityLoadedMessage = "Availability loaded.";
}
