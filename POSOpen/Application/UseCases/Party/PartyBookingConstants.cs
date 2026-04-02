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
	public const string ErrorInventoryReservationFailed = "BOOKING_INVENTORY_RESERVATION_FAILED";
	public const string ErrorInventoryReleaseFailed = "BOOKING_INVENTORY_RELEASE_FAILED";
	public const string ErrorInventoryFinalizationBlocked = "BOOKING_INVENTORY_FINALIZATION_BLOCKED";

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
	public const string SafeInventoryReservationFailedMessage = "Inventory reservation failed. Please try again.";
	public const string SafeInventoryReleaseFailedMessage = "Inventory release failed. Please try again.";
	public const string SafeInventoryFinalizationBlockedMessage = "Resolve inventory constraints before finalizing this booking.";

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

	public static readonly string[] KnownCateringOptionIds =
	[
		"pizza-basic",
		"pizza-deluxe",
		"fruit-platter",
		"veggie-platter",
		"cake-standard",
		"cake-custom",
	];

	public static readonly string[] KnownDecorOptionIds =
	[
		"balloon-basic",
		"balloon-premium",
		"table-standard",
		"table-themed",
		"banner-standard",
		"banner-custom",
	];

	public static readonly IReadOnlyDictionary<string, long> AddOnOptionPriceCents = new Dictionary<string, long>
	{
		["pizza-basic"] = 2500,
		["pizza-deluxe"] = 4500,
		["fruit-platter"] = 1800,
		["veggie-platter"] = 1600,
		["cake-standard"] = 3500,
		["cake-custom"] = 6500,
		["balloon-basic"] = 800,
		["balloon-premium"] = 1500,
		["table-standard"] = 0,
		["table-themed"] = 1200,
		["banner-standard"] = 0,
		["banner-custom"] = 900,
	};

	public static readonly IReadOnlyDictionary<string, string> AddOnOptionDisplayNames = new Dictionary<string, string>
	{
		["pizza-basic"] = "Pizza (Basic)",
		["pizza-deluxe"] = "Pizza (Deluxe)",
		["fruit-platter"] = "Fruit Platter",
		["veggie-platter"] = "Veggie Platter",
		["cake-standard"] = "Cake (Standard)",
		["cake-custom"] = "Cake (Custom)",
		["balloon-basic"] = "Balloons (Basic)",
		["balloon-premium"] = "Balloons (Premium)",
		["table-standard"] = "Table Setup (Standard)",
		["table-themed"] = "Table Setup (Themed)",
		["banner-standard"] = "Banner (Standard)",
		["banner-custom"] = "Banner (Custom)",
	};

	public static readonly IReadOnlySet<string> KnownAtRiskOptionIds = new HashSet<string>
	{
		"cake-custom",
		"balloon-premium",
		"banner-custom",
		"table-themed",
	};

	public const string RiskSeverityLow = "Low";
	public const string RiskSeverityHigh = "High";

	public const string RiskReasonInventoryShortfall = "Inventory shortfall risk for this option near event date.";
	public const string RiskReasonPolicyConflict = "This option may conflict with booking policy rules.";

	public static readonly IReadOnlyDictionary<string, (string Severity, string Reason)> AtRiskOptionMeta =
		new Dictionary<string, (string, string)>
		{
			["cake-custom"] = (RiskSeverityHigh, RiskReasonInventoryShortfall),
			["balloon-premium"] = (RiskSeverityHigh, RiskReasonInventoryShortfall),
			["banner-custom"] = (RiskSeverityLow, RiskReasonPolicyConflict),
			["table-themed"] = (RiskSeverityLow, RiskReasonPolicyConflict),
		};

	public const string ErrorAddOnUpdateFailed = "BOOKING_ADDON_UPDATE_FAILED";
	public const string ErrorAddOnOptionInvalid = "BOOKING_ADDON_OPTION_INVALID";
	public const string SafeAddOnUpdateFailedMessage = "Failed to save add-on selections. Please try again.";
	public const string SafeAddOnOptionInvalidMessage = "One or more selected options are not valid.";
	public const string AddOnSelectionsUpdatedMessage = "Catering and decor options saved.";
	public const string AddOnSelectionsAlreadySavedMessage = "Add-on selections were already saved for this operation.";
	public const string AddOnOptionsLoadedMessage = "Add-on options loaded.";

	public const string InventoryReservationSavedMessage = "Inventory reservation updated.";
	public const string InventoryReservationSavedWithConstraintsMessage = "Inventory reservation saved with unresolved constraints.";
	public const string InventoryReservationSatisfiedMessage = "Inventory requirements are fully reserved.";
	public const string InventoryConstraintGuidanceMessage = "Review constrained items and choose an allowed substitute.";
	public const string InventoryReleaseAppliedMessage = "Inventory release policy applied.";
	public const string InventorySubstitutesLoadedMessage = "Substitute options loaded.";

	public const int DefaultInventoryCapacity = 20;

	public static readonly IReadOnlyDictionary<string, int> InventoryCapacityByOption = new Dictionary<string, int>
	{
		["pizza-basic"] = 30,
		["pizza-deluxe"] = 20,
		["fruit-platter"] = 15,
		["veggie-platter"] = 15,
		["cake-standard"] = 12,
		["cake-custom"] = 2,
		["balloon-basic"] = 18,
		["balloon-premium"] = 2,
		["table-standard"] = 12,
		["table-themed"] = 1,
		["banner-standard"] = 12,
		["banner-custom"] = 1,
	};
}
