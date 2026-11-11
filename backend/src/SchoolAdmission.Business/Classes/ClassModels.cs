namespace SchoolAdmission.Business.Classes;

public record CreateClassRequest(string? Name, int? TotalSeats);

public record ClassSeats(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats)
{
    public int AvailableSeats => TotalSeats - HeldSeats - ConfirmedSeats;
}
