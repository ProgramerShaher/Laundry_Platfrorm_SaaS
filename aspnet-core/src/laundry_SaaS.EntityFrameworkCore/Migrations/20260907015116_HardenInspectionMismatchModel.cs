using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laundry_SaaS.Migrations
{
    /// <inheritdoc />
    public partial class HardenInspectionMismatchModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "PickupSchedule_StartTime",
                table: "AppOrders",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PickupSchedule_ScheduledDate",
                table: "AppOrders",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PickupSchedule_OriginalSlotId",
                table: "AppOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "PickupSchedule_EndTime",
                table: "AppOrders",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderItemId",
                table: "AppInspectionItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ActualLaundryItemTypeId",
                table: "AppInspectionItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActualLaundryServiceId",
                table: "AppInspectionItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExpectedLaundryItemTypeId",
                table: "AppInspectionItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExpectedLaundryServiceId",
                table: "AppInspectionItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppInspectionItems_OrderItemId",
                table: "AppInspectionItems",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppInspectionItems_OrderItemId",
                table: "AppInspectionItems");

            migrationBuilder.DropColumn(
                name: "ActualLaundryItemTypeId",
                table: "AppInspectionItems");

            migrationBuilder.DropColumn(
                name: "ActualLaundryServiceId",
                table: "AppInspectionItems");

            migrationBuilder.DropColumn(
                name: "ExpectedLaundryItemTypeId",
                table: "AppInspectionItems");

            migrationBuilder.DropColumn(
                name: "ExpectedLaundryServiceId",
                table: "AppInspectionItems");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "PickupSchedule_StartTime",
                table: "AppOrders",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PickupSchedule_ScheduledDate",
                table: "AppOrders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<Guid>(
                name: "PickupSchedule_OriginalSlotId",
                table: "AppOrders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "PickupSchedule_EndTime",
                table: "AppOrders",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderItemId",
                table: "AppInspectionItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
