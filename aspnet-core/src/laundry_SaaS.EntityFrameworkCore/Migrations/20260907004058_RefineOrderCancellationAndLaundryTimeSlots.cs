using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laundry_SaaS.Migrations
{
    /// <inheritdoc />
    public partial class RefineOrderCancellationAndLaundryTimeSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppLaundryTimeSlots_LaundryId_SlotType",
                table: "AppLaundryTimeSlots");

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "AppLaundryTimeSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryTimeSlots_LaundryId_DayOfWeek_SlotType_StartTime_EndTime",
                table: "AppLaundryTimeSlots",
                columns: new[] { "LaundryId", "DayOfWeek", "SlotType", "StartTime", "EndTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppLaundryTimeSlots_LaundryId_DayOfWeek_SlotType_StartTime_EndTime",
                table: "AppLaundryTimeSlots");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "AppLaundryTimeSlots");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaundryTimeSlots_LaundryId_SlotType",
                table: "AppLaundryTimeSlots",
                columns: new[] { "LaundryId", "SlotType" });
        }
    }
}
