using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laundry_SaaS.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryVerificationAndPickupSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "PickupSchedule_EndTime",
                table: "AppOrders",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PickupSchedule_OriginalSlotId",
                table: "AppOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PickupSchedule_ScheduledDate",
                table: "AppOrders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "PickupSchedule_StartTime",
                table: "AppOrders",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationInfo_ExpiresAt",
                table: "AppDeliveryTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationInfo_FailedAttempts",
                table: "AppDeliveryTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VerificationInfo_GenerationCount",
                table: "AppDeliveryTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "VerificationInfo_IsVerified",
                table: "AppDeliveryTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationInfo_LastSentAt",
                table: "AppDeliveryTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationInfo_MaxAttempts",
                table: "AppDeliveryTasks",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "VerificationInfo_OtpHash",
                table: "AppDeliveryTasks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationInfo_VerifiedAt",
                table: "AppDeliveryTasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupSchedule_EndTime",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "PickupSchedule_OriginalSlotId",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "PickupSchedule_ScheduledDate",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "PickupSchedule_StartTime",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_ExpiresAt",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_FailedAttempts",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_GenerationCount",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_IsVerified",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_LastSentAt",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_MaxAttempts",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_OtpHash",
                table: "AppDeliveryTasks");

            migrationBuilder.DropColumn(
                name: "VerificationInfo_VerifiedAt",
                table: "AppDeliveryTasks");
        }
    }
}
