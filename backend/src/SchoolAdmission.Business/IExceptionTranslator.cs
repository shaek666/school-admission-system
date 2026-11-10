using SchoolAdmission.Business.Errors;

namespace SchoolAdmission.Business;

/// <summary>Maps a storage exception to an <see cref="AppException"/>. Returns null when it has no mapping.</summary>
public interface IExceptionTranslator
{
    AppException? Translate(Exception exception);
}
