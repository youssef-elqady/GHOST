using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GHOST.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentShiftLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ShiftId",
                table: "Payments",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Shifts_ShiftId",
                table: "Payments",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Shifts_ShiftId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ShiftId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Payments");
        }
    }
}
