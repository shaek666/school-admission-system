using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holds", x => x.id);
                    table.CheckConstraint("ck_holds_student_id_format", "student_id ~ '^[0-9]{17}$'");
                    table.ForeignKey(
                        name: "fk_holds_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_holds_class_id",
                table: "holds",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "ux_holds_student_class_active",
                table: "holds",
                columns: new[] { "student_id", "class_id" },
                unique: true,
                filter: "status IN ('HELD','CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holds");

            migrationBuilder.DropTable(
                name: "classes");
        }
    }
}
