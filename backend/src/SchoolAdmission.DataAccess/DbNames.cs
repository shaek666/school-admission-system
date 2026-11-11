namespace SchoolAdmission.DataAccess;

/// <summary>Database object names that code outside the EF configuration also needs.</summary>
public static class DbNames
{
    /// <summary>Unique index that allows one active (HELD or CONFIRMED) hold per student and class.</summary>
    public const string ActiveHoldIndex = "ux_holds_student_class_active";

    public const string StudentCreatedIndex = "ix_holds_student_created";
    public const string ClassStatusIndex = "ix_holds_class_status";
    public const string TotalSeatsCheck = "ck_classes_total_seats_range";
    public const string ClassNameCheck = "ck_classes_name_length";
}
