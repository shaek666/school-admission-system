using System.Globalization;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Classes;

namespace SchoolAdmission.Api.Http;

public record ClassResponse(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats)
{
    public static ClassResponse From(ClassSeats c) =>
        new(c.Id.ToString(), c.Name, c.TotalSeats, c.HeldSeats, c.ConfirmedSeats, c.AvailableSeats);
}

public record HoldCreatedResponse(string HoldId, string ClassId, string StudentId, string Status, string ExpiresAt)
{
    private const string ExpiresAtFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public static HoldCreatedResponse From(HoldRecord h) =>
        new(
            h.Id.ToString(),
            h.ClassId.ToString(),
            h.StudentId,
            h.Status,
            h.ExpiresAtUtc.ToUniversalTime().ToString(ExpiresAtFormat, CultureInfo.InvariantCulture));
}
