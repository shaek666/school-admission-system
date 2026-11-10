namespace SchoolAdmission.Business.Errors;

/// <summary>The only way to create an <see cref="AppException"/>. Codes match the design status map.</summary>
public static class Errors
{
    public static AppException Validation(string message) =>
        new(ErrorKind.Validation, "VALIDATION_ERROR", message);

    public static AppException InvalidJson() =>
        new(ErrorKind.Validation, "INVALID_JSON", "The request body is not valid JSON for this request.");

    public static AppException InvalidStudentId() =>
        new(ErrorKind.Validation, "INVALID_STUDENT_ID", "The student ID must be exactly 17 digits.");

    public static AppException ClassNotFound() =>
        new(ErrorKind.NotFound, "CLASS_NOT_FOUND", "We could not find this class.");

    public static AppException HoldNotFound() =>
        new(ErrorKind.NotFound, "HOLD_NOT_FOUND", "We could not find this application.");

    public static AppException NotHoldOwner() =>
        new(ErrorKind.Forbidden, "NOT_HOLD_OWNER", "This application belongs to another student.");

    public static AppException NoSeatsAvailable() =>
        new(ErrorKind.Conflict, "NO_SEATS_AVAILABLE", "No seats are left in this class.");

    public static AppException AlreadyApplied() =>
        new(ErrorKind.Conflict, "ALREADY_APPLIED", "This student already has an application for this class.");

    public static AppException HoldExpired() =>
        new(ErrorKind.Conflict, "HOLD_EXPIRED", "This seat hold has expired.");

    public static AppException HoldCancelled() =>
        new(ErrorKind.Conflict, "HOLD_CANCELLED", "This application was cancelled.");

    public static AppException HoldConfirmed() =>
        new(ErrorKind.Conflict, "HOLD_CONFIRMED", "This application is already confirmed.");

    public static AppException ServiceBusy() =>
        new(ErrorKind.Busy, "SERVICE_BUSY", "The service is busy. Please try again in a moment.");
}
