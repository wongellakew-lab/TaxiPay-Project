using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxiPayApi.Domain.Entities;

#nullable disable

namespace TaxiPayApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "passengers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    total_trips = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_spent = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passengers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    route_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nodes = table.Column<List<Node>>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    license_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    photo_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    total_trips = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_revenue = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_Drivers_routes_CurrentRouteId",
                        column: x => x.CurrentRouteId,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FareMatrices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_node = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    to_node = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    fare = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareMatrices", x => x.id);
                    table.ForeignKey(
                        name: "FK_FareMatrices_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "driver_shifts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_trip_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_passengers = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_revenue = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_shifts", x => x.id);
                    table.ForeignKey(
                        name: "FK_driver_shifts_Drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "Drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_driver_shifts_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_change_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reviewed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_route_change_requests_Drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "Drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_change_requests_admins_reviewed_by_admin_id",
                        column: x => x.reviewed_by_admin_id,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_change_requests_routes_current_route_id",
                        column: x => x.current_route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_change_requests_routes_new_route_id",
                        column: x => x.new_route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    trip_number = table.Column<int>(type: "integer", nullable: false),
                    trip_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trip_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    has_reached_final_destination = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    passengers_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    trip_revenue = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.id);
                    table.ForeignKey(
                        name: "FK_trips_Drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "Drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_driver_shifts_driver_shift_id",
                        column: x => x.driver_shift_id,
                        principalTable: "driver_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolution_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.id);
                    table.ForeignKey(
                        name: "FK_disputes_Drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "Drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_disputes_admins_resolved_by_admin_id",
                        column: x => x.resolved_by_admin_id,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_disputes_passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_disputes_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "passenger_journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    boarding_node = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    destination_node = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    fare = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transaction_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receipt_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_journeys", x => x.id);
                    table.ForeignKey(
                        name: "FK_passenger_journeys_passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_passenger_journeys_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    receipt_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivery_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_receipts_passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receipts_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admins_email",
                table: "admins",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admins_phone",
                table: "admins",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_disputes_passenger",
                table: "disputes",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "idx_disputes_status",
                table: "disputes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_driver_id",
                table: "disputes",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_resolved_by_admin_id",
                table: "disputes",
                column: "resolved_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_trip_id",
                table: "disputes",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_driver_shifts_driver_status",
                table: "driver_shifts",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_driver_shifts_start",
                table: "driver_shifts",
                column: "shift_start");

            migrationBuilder.CreateIndex(
                name: "IX_driver_shifts_route_id",
                table: "driver_shifts",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CurrentRouteId",
                table: "Drivers",
                column: "CurrentRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_driver_id",
                table: "Drivers",
                column: "driver_id",
                unique: true,
                filter: "\"driver_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_license_number",
                table: "Drivers",
                column: "license_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_phone",
                table: "Drivers",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_fare_matrix",
                table: "FareMatrices",
                columns: new[] { "route_id", "from_node", "to_node" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_passenger_journeys_passenger",
                table: "passenger_journeys",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "idx_passenger_journeys_trip_passenger",
                table: "passenger_journeys",
                columns: new[] { "trip_id", "passenger_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_passengers_phone",
                table: "passengers",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipts_passenger_id",
                table: "receipts",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "uq_receipts_trip",
                table: "receipts",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_route_change_driver_status",
                table: "route_change_requests",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_route_change_requests_current_route_id",
                table: "route_change_requests",
                column: "current_route_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_change_requests_new_route_id",
                table: "route_change_requests",
                column: "new_route_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_change_requests_reviewed_by_admin_id",
                table: "route_change_requests",
                column: "reviewed_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "idx_routes_code",
                table: "routes",
                column: "route_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_routes_name",
                table: "routes",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_trips_driver_status",
                table: "trips",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_trips_shift_status",
                table: "trips",
                columns: new[] { "driver_shift_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_trips_start",
                table: "trips",
                column: "trip_start");

            migrationBuilder.CreateIndex(
                name: "IX_trips_route_id",
                table: "trips",
                column: "route_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "FareMatrices");

            migrationBuilder.DropTable(
                name: "passenger_journeys");

            migrationBuilder.DropTable(
                name: "receipts");

            migrationBuilder.DropTable(
                name: "route_change_requests");

            migrationBuilder.DropTable(
                name: "passengers");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "admins");

            migrationBuilder.DropTable(
                name: "driver_shifts");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "routes");
        }
    }
}
