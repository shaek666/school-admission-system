namespace SchoolAdmission.DataAccess;

/// <summary>Database object names that code outside the EF configuration also needs.</summary>
public static class DbNames
{
    /// <summary>Unique index that allows one active (HELD or CONFIRMED) hold per student and class.</summary>
    public const string ActiveHoldIndex = "ux_holds_student_class_active";
}
