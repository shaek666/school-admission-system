using SchoolAdmission.Business.Classes;

namespace SchoolAdmission.Api.Http;

public record ClassResponse(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats)
{
    public static ClassResponse From(ClassSeats c) =>
        new(c.Id.ToString(), c.Name, c.TotalSeats, c.HeldSeats, c.ConfirmedSeats, c.AvailableSeats);
}
