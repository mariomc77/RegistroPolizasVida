using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistroPolizasVida.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InicialCreacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LotesCarga",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreArchivoZip = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicioProcesamiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinProcesamiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalArchivos = table.Column<int>(type: "int", nullable: false),
                    ArchivosProcesados = table.Column<int>(type: "int", nullable: false),
                    TotalPolizas = table.Column<int>(type: "int", nullable: false),
                    PolizasInsertadas = table.Column<int>(type: "int", nullable: false),
                    PolizasActualizadas = table.Column<int>(type: "int", nullable: false),
                    PolizasConError = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesCarga", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Polizas",
                columns: table => new
                {
                    NumeroPoliza = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    MontoCobertura = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Tomador_TipoPersona = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tomador_Cedula = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Tomador_PrimerApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tomador_SegundoApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tomador_CedulaJuridica = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Tomador_RazonSocial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Tomador_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tomador_Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Tomador_Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Tomador_Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LoteCargaOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteCargaUltimaActualizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersionActualizacion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polizas", x => x.NumeroPoliza);
                });

            migrationBuilder.CreateTable(
                name: "ArchivosLote",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteCargaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CantidadPolizas = table.Column<int>(type: "int", nullable: false),
                    PolizasInsertadas = table.Column<int>(type: "int", nullable: false),
                    PolizasActualizadas = table.Column<int>(type: "int", nullable: false),
                    PolizasConError = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosLote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivosLote_LotesCarga_LoteCargaId",
                        column: x => x.LoteCargaId,
                        principalTable: "LotesCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asegurados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimerApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SegundoApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PolizaNumero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asegurados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asegurados_Polizas_PolizaNumero",
                        column: x => x.PolizaNumero,
                        principalTable: "Polizas",
                        principalColumn: "NumeroPoliza",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErroresProcesamiento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchivoLoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroPoliza = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Linea = table.Column<int>(type: "int", nullable: true),
                    FechaError = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErroresProcesamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErroresProcesamiento_ArchivosLote_ArchivoLoteId",
                        column: x => x.ArchivoLoteId,
                        principalTable: "ArchivosLote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beneficiarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoPersona = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    PrimerApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SegundoApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CedulaJuridica = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    RazonSocial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PorcentajeBeneficio = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AseguradoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiarios_Asegurados_AseguradoId",
                        column: x => x.AseguradoId,
                        principalTable: "Asegurados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosLote_LoteCargaId",
                table: "ArchivosLote",
                column: "LoteCargaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asegurados_Cedula",
                table: "Asegurados",
                column: "Cedula");

            migrationBuilder.CreateIndex(
                name: "IX_Asegurados_PolizaNumero",
                table: "Asegurados",
                column: "PolizaNumero");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiarios_AseguradoId",
                table: "Beneficiarios",
                column: "AseguradoId");

            migrationBuilder.CreateIndex(
                name: "IX_ErroresProcesamiento_ArchivoLoteId",
                table: "ErroresProcesamiento",
                column: "ArchivoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesCarga_FechaCarga",
                table: "LotesCarga",
                column: "FechaCarga");

            migrationBuilder.CreateIndex(
                name: "IX_Polizas_LoteCargaOrigenId",
                table: "Polizas",
                column: "LoteCargaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Polizas_LoteCargaUltimaActualizacionId",
                table: "Polizas",
                column: "LoteCargaUltimaActualizacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beneficiarios");

            migrationBuilder.DropTable(
                name: "ErroresProcesamiento");

            migrationBuilder.DropTable(
                name: "Asegurados");

            migrationBuilder.DropTable(
                name: "ArchivosLote");

            migrationBuilder.DropTable(
                name: "Polizas");

            migrationBuilder.DropTable(
                name: "LotesCarga");
        }
    }
}
