using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HoldIndexesAndChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_holds_class_status",
                table: "holds",
                columns: new[] { "class_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_holds_student_created",
                table: "holds",
                columns: new[] { "student_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.AddCheckConstraint(
                name: "ck_classes_name_length",
                table: "classes",
                sql: "char_length(name) BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "ck_classes_total_seats_range",
                table: "classes",
                sql: "total_seats BETWEEN 1 AND 100000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_holds_class_status",
                table: "holds");

            migrationBuilder.DropIndex(
                name: "ix_holds_student_created",
                table: "holds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_classes_name_length",
                table: "classes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_classes_total_seats_range",
                table: "classes");
        }
    }
}
