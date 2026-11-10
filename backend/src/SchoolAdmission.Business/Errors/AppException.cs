namespace SchoolAdmission.Business;

public sealed class AppException(ErrorKind kind, string code, string message) : Exception(message)
{
    public ErrorKind Kind { get; } = kind;

    public string Code { get; } = code;
}
