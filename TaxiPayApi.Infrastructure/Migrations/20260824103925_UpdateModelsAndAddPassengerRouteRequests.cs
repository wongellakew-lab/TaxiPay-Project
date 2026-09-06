using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiPayApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsAndAddPassengerRouteRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "uq_receipts_trip",
                table: "receipts",
                newName: "idx_receipts_trip");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_passenger_id",
                table: "receipts",
                newName: "idx_receipts_passenger");

            migrationBuilder.AlterColumn<string>(
                name: "passenger_phone",
                table: "receipts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "boarding_node",
                table: "receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_node",
                table: "receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "driver_code",
                table: "receipts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "driver_name",
                table: "receipts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "fare",
                table: "receipts",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "journey_id",
                table: "receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "passenger_name",
                table: "receipts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "receipts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "receipts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "receipt_number",
                table: "receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "route_name",
                table: "receipts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "transaction_ref",
                table: "receipts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "alighted_at",
                table: "passenger_journeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alighting_status",
                table: "passenger_journeys",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "OnBoard");

            migrationBuilder.AddColumn<Guid>(
                name: "preferred_route_id",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "Drivers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "passenger_route_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    to_location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    admin_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reviewed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_route_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_passenger_route_requests_admins_reviewed_by_admin_id",
                        column: x => x.reviewed_by_admin_id,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_passenger_route_requests_passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_receipts_number",
                table: "receipts",
                column: "receipt_number");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_journey_id",
                table: "receipts",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_preferred_route_id",
                table: "Drivers",
                column: "preferred_route_id");

            migrationBuilder.CreateIndex(
                name: "idx_prr_passenger",
                table: "passenger_route_requests",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "idx_prr_status",
                table: "passenger_route_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_route_requests_reviewed_by_admin_id",
                table: "passenger_route_requests",
                column: "reviewed_by_admin_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_routes_preferred_route_id",
                table: "Drivers",
                column: "preferred_route_id",
                principalTable: "routes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_receipts_passenger_journeys_journey_id",
                table: "receipts",
                column: "journey_id",
                principalTable: "passenger_journeys",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_routes_preferred_route_id",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_receipts_passenger_journeys_journey_id",
                table: "receipts");

            migrationBuilder.DropTable(
                name: "passenger_route_requests");

            migrationBuilder.DropIndex(
                name: "idx_receipts_number",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_journey_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_preferred_route_id",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "boarding_node",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "destination_node",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "driver_code",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "driver_name",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "fare",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "journey_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "passenger_name",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "receipt_number",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "route_name",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "transaction_ref",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "alighted_at",
                table: "passenger_journeys");

            migrationBuilder.DropColumn(
                name: "alighting_status",
                table: "passenger_journeys");

            migrationBuilder.DropColumn(
                name: "preferred_route_id",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "Drivers");

            migrationBuilder.RenameIndex(
                name: "idx_receipts_trip",
                table: "receipts",
                newName: "uq_receipts_trip");

            migrationBuilder.RenameIndex(
                name: "idx_receipts_passenger",
                table: "receipts",
                newName: "IX_receipts_passenger_id");

            migrationBuilder.AlterColumn<string>(
                name: "passenger_phone",
                table: "receipts",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
