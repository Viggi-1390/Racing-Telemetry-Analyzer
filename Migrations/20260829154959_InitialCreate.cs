using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RacingTelemetryAnalyzer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    TrackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Length = table.Column<double>(type: "float", nullable: false),
                    NumberOfTurns = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.TrackId);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    VehicleCategory = table.Column<int>(type: "int", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Engine = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Power = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    TopSpeed = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                });

            migrationBuilder.CreateTable(
                name: "TrackSections",
                columns: table => new
                {
                    TrackSectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDistance = table.Column<double>(type: "float", nullable: false),
                    EndDistance = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackSections", x => x.TrackSectionId);
                    table.ForeignKey(
                        name: "FK_TrackSections_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "TrackId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    TrackId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "TrackId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Laps",
                columns: table => new
                {
                    LapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    LapNumber = table.Column<int>(type: "int", nullable: false),
                    LapTime = table.Column<long>(type: "bigint", nullable: false),
                    Sector1Time = table.Column<double>(type: "float", nullable: false),
                    Sector2Time = table.Column<double>(type: "float", nullable: false),
                    Sector3Time = table.Column<double>(type: "float", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laps", x => x.LapId);
                    table.ForeignKey(
                        name: "FK_Laps_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionConditions",
                columns: table => new
                {
                    SessionConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    AirTemperature = table.Column<double>(type: "float", nullable: false),
                    TrackTemperature = table.Column<double>(type: "float", nullable: false),
                    Weather = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WindSpeed = table.Column<double>(type: "float", nullable: false),
                    TrackCondition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionConditions", x => x.SessionConditionId);
                    table.ForeignKey(
                        name: "FK_SessionConditions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionNotes",
                columns: table => new
                {
                    SessionNoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    LapId = table.Column<int>(type: "int", nullable: true),
                    TrackSectionId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionNotes", x => x.SessionNoteId);
                    table.ForeignKey(
                        name: "FK_SessionNotes_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleSetups",
                columns: table => new
                {
                    VehicleSetupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    FrontWing = table.Column<double>(type: "float", nullable: false),
                    RearWing = table.Column<double>(type: "float", nullable: false),
                    RideHeight = table.Column<double>(type: "float", nullable: false),
                    BrakeBias = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleSetups", x => x.VehicleSetupId);
                    table.ForeignKey(
                        name: "FK_VehicleSetups_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    AnalysisResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LapId = table.Column<int>(type: "int", nullable: false),
                    TrackSectionId = table.Column<int>(type: "int", nullable: true),
                    EntrySpeed = table.Column<double>(type: "float", nullable: false),
                    MinimumSpeed = table.Column<double>(type: "float", nullable: false),
                    ExitSpeed = table.Column<double>(type: "float", nullable: false),
                    BrakingPercentage = table.Column<double>(type: "float", nullable: false),
                    ThrottleResponseTime = table.Column<double>(type: "float", nullable: false),
                    TimeDelta = table.Column<double>(type: "float", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendationType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.AnalysisResultId);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_Laps_LapId",
                        column: x => x.LapId,
                        principalTable: "Laps",
                        principalColumn: "LapId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_TrackSections_TrackSectionId",
                        column: x => x.TrackSectionId,
                        principalTable: "TrackSections",
                        principalColumn: "TrackSectionId");
                });

            migrationBuilder.CreateTable(
                name: "TelemetryPoints",
                columns: table => new
                {
                    TelemetryPointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LapId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Distance = table.Column<double>(type: "float", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    RPM = table.Column<int>(type: "int", nullable: false),
                    Gear = table.Column<int>(type: "int", nullable: false),
                    Throttle = table.Column<double>(type: "float", nullable: false),
                    Brake = table.Column<double>(type: "float", nullable: false),
                    Sector = table.Column<int>(type: "int", nullable: false),
                    Steering = table.Column<double>(type: "float", nullable: true),
                    SuspensionFL = table.Column<double>(type: "float", nullable: true),
                    SuspensionFR = table.Column<double>(type: "float", nullable: true),
                    SuspensionRL = table.Column<double>(type: "float", nullable: true),
                    SuspensionRR = table.Column<double>(type: "float", nullable: true),
                    LeanAngle = table.Column<double>(type: "float", nullable: true),
                    FrontBrake = table.Column<double>(type: "float", nullable: true),
                    RearBrake = table.Column<double>(type: "float", nullable: true),
                    FrontSuspension = table.Column<double>(type: "float", nullable: true),
                    RearSuspension = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryPoints", x => x.TelemetryPointId);
                    table.ForeignKey(
                        name: "FK_TelemetryPoints_Laps_LapId",
                        column: x => x.LapId,
                        principalTable: "Laps",
                        principalColumn: "LapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_LapId",
                table: "AnalysisResults",
                column: "LapId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_TrackSectionId",
                table: "AnalysisResults",
                column: "TrackSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Laps_SessionId",
                table: "Laps",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionConditions_SessionId",
                table: "SessionConditions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionNotes_SessionId",
                table: "SessionNotes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TrackId",
                table: "Sessions",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_VehicleId",
                table: "Sessions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryPoints_LapId",
                table: "TelemetryPoints",
                column: "LapId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackSections_TrackId",
                table: "TrackSections",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSetups_SessionId",
                table: "VehicleSetups",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisResults");

            migrationBuilder.DropTable(
                name: "SessionConditions");

            migrationBuilder.DropTable(
                name: "SessionNotes");

            migrationBuilder.DropTable(
                name: "TelemetryPoints");

            migrationBuilder.DropTable(
                name: "VehicleSetups");

            migrationBuilder.DropTable(
                name: "TrackSections");

            migrationBuilder.DropTable(
                name: "Laps");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
