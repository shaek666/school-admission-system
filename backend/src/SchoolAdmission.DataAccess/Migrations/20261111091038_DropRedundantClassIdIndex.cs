using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropRedundantClassIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_holds_class_id",
                table: "holds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_holds_class_id",
                table: "holds",
                column: "class_id");
        }
    }
}
