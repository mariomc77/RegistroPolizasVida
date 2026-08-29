using System.IO.Compression;
using RegistroPolizasVida.Application.Common;
using RegistroPolizasVida.Application.Notifications;
using RegistroPolizasVida.Application.Parsing;
using RegistroPolizasVida.Application.Services;
using RegistroPolizasVida.Application.Validation;
using RegistroPolizasVida.Application.Zip;
using RegistroPolizasVida.Domain.Enums;
using RegistroPolizasVida.SelfCheck.InMemory;

// ============================================================================
// Autochequeo de la lógica de negocio (Domain + Application) contra los
// archivos de ejemplo reales del reto, SIN necesitar SQL Server ni EF Core
// (se usa un repositorio en memoria -- ver InMemory/InMemoryUnitOfWork.cs).
//
// Simula el escenario real descrito por el profesor con dos cargas (.ZIP)
// SEPARADAS, una después de la otra, tal como ocurriría con dos subidas
// reales del usuario:
//
//   Carga 1: lote_01, lote_02, lote_03, lote_04 (válidos) + lote_06 (inválido)
//   Carga 2: lote_05 (actualiza la póliza POL-001-2026 creada en la carga 1)
// ============================================================================

var directorioMuestras = AppContext.BaseDirectory;
var fallos = new List<string>();

void Assert(bool condicion, string descripcion)
{
    if (condicion)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  [OK] {descripcion}");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [FALLO] {descripcion}");
        fallos.Add(descripcion);
    }
    Console.ResetColor();
}

var store = new InMemoryStore();
var unitOfWorkFactory = new InMemoryUnitOfWorkFactory(store);

var servicio = new BatchProcessingService(
    unitOfWorkFactory,
    new ZipBatchExtractor(),
    new XsdBatchValidator(),
    new PolizaXmlParser(),
    new PolizaBusinessRuleValidator(),
    new NullProcesamientoNotifier(),
    new ConsolePipelineLogger<BatchProcessingService>());

var lotesApp = new LoteCargaAppService(unitOfWorkFactory);

byte[] EmpacarZip(params string[] nombresArchivo)
{
    using var memoria = new MemoryStream();
    using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var nombre in nombresArchivo)
        {
            var ruta = Path.Combine(directorioMuestras, nombre);
            var entrada = zip.CreateEntry(nombre);
            using var destino = entrada.Open();
            using var origen = File.OpenRead(ruta);
            origen.CopyTo(destino);
        }
    }
    return memoria.ToArray();
}

Console.WriteLine("=====================================================================");
Console.WriteLine(" CARGA 1: polizas_lote_01..04.xml (válidos) + polizas_lote_06.xml (inválido)");
Console.WriteLine("=====================================================================");

var zip1 = EmpacarZip("polizas_lote_01.xml", "polizas_lote_02.xml", "polizas_lote_03.xml", "polizas_lote_04.xml", "polizas_lote_06.xml");
var lote1 = await lotesApp.CrearLoteAsync("carga1.zip");
var resultado1 = await servicio.ProcesarLoteAsync(lote1.Id, zip1);

Console.WriteLine();
Console.WriteLine($"Estado del lote: {resultado1.Estado}  |  Archivos: {resultado1.TotalArchivos}  |  " +
                   $"Pólizas: {resultado1.TotalPolizas} (nuevas {resultado1.PolizasInsertadas}, " +
                   $"actualizadas {resultado1.PolizasActualizadas}, con error {resultado1.PolizasConError})");
foreach (var archivo in resultado1.Archivos.OrderBy(a => a.NombreArchivo))
{
    Console.WriteLine($"  - {archivo.NombreArchivo}: {archivo.Estado} " +
                       $"(pólizas={archivo.CantidadPolizas}, insertadas={archivo.PolizasInsertadas}, " +
                       $"actualizadas={archivo.PolizasActualizadas}, error={archivo.PolizasConError}, " +
                       $"hallazgos={archivo.Errores.Count})");
    foreach (var error in archivo.Errores.Take(10))
        Console.WriteLine($"        · [{error.Tipo}] {error.Mensaje}");
}

Console.WriteLine();
Console.WriteLine("Verificaciones carga 1:");
Assert(resultado1.Estado == EstadoLote.CompletadoConErrores,
    $"El lote queda en estado CompletadoConErrores (uno de cinco archivos es inválido a propósito). Estado real: {resultado1.Estado}");
Assert(resultado1.Archivos.Count == 5, $"Se procesaron los 5 archivos del .ZIP. Cantidad real: {resultado1.Archivos.Count}");

var archivo01 = resultado1.Archivos.Single(a => a.NombreArchivo == "polizas_lote_01.xml");
Assert(archivo01.Estado == EstadoArchivo.Procesado, $"lote_01 se procesa sin errores. Estado real: {archivo01.Estado}");
Assert(archivo01.PolizasInsertadas == 1, $"lote_01 inserta 1 póliza nueva (POL-001-2026). Real: {archivo01.PolizasInsertadas}");

var archivo02 = resultado1.Archivos.Single(a => a.NombreArchivo == "polizas_lote_02.xml");
Assert(archivo02.Estado == EstadoArchivo.Procesado, $"lote_02 (tomador = asegurado) se procesa sin errores. Estado real: {archivo02.Estado}");

var archivo03 = resultado1.Archivos.Single(a => a.NombreArchivo == "polizas_lote_03.xml");
Assert(archivo03.Estado == EstadoArchivo.Procesado, $"lote_03 (colectiva, 3 asegurados) se procesa sin errores. Estado real: {archivo03.Estado}");

var archivo04 = resultado1.Archivos.Single(a => a.NombreArchivo == "polizas_lote_04.xml");
Assert(archivo04.Estado == EstadoArchivo.Procesado, $"lote_04 (individual + colectiva en un solo XML) se procesa sin errores. Estado real: {archivo04.Estado}");
Assert(archivo04.PolizasInsertadas == 2, $"lote_04 inserta sus 2 pólizas. Real: {archivo04.PolizasInsertadas}");

var archivo06 = resultado1.Archivos.Single(a => a.NombreArchivo == "polizas_lote_06.xml");
Assert(archivo06.Estado == EstadoArchivo.InvalidoEsquema, $"lote_06 (diseñado para fallar el XSD) se marca InvalidoEsquema. Estado real: {archivo06.Estado}");
Assert(archivo06.Errores.Count >= 5, $"lote_06 reporta varios errores de esquema distintos (el archivo documenta 7). Cantidad real: {archivo06.Errores.Count}");

var polizaOriginal = store.Polizas["POL-001-2026"];
Assert(polizaOriginal.MontoCobertura == 25000000.00m, $"POL-001-2026 queda con el monto original de lote_01 (25,000,000). Real: {polizaOriginal.MontoCobertura}");
Assert(polizaOriginal.Asegurados.Single().Beneficiarios.Count == 2, "POL-001-2026 tiene 2 beneficiarios antes de la actualización.");

Console.WriteLine();
Console.WriteLine("=====================================================================");
Console.WriteLine(" CARGA 2 (subida posterior, por separado): polizas_lote_05.xml");
Console.WriteLine(" -> debe RECONOCER que POL-001-2026 ya existe y REEMPLAZAR sus datos");
Console.WriteLine("=====================================================================");

var zip2 = EmpacarZip("polizas_lote_05.xml");
var lote2 = await lotesApp.CrearLoteAsync("carga2.zip");
var resultado2 = await servicio.ProcesarLoteAsync(lote2.Id, zip2);

Console.WriteLine();
Console.WriteLine($"Estado del lote: {resultado2.Estado}  |  Pólizas: {resultado2.TotalPolizas} " +
                   $"(nuevas {resultado2.PolizasInsertadas}, actualizadas {resultado2.PolizasActualizadas})");

Console.WriteLine();
Console.WriteLine("Verificaciones carga 2:");
Assert(resultado2.Estado == EstadoLote.Completado, $"El lote 2 se completa sin errores. Estado real: {resultado2.Estado}");
Assert(resultado2.PolizasActualizadas == 1 && resultado2.PolizasInsertadas == 0,
    $"POL-001-2026 se detecta como ACTUALIZACIÓN, no como inserción nueva. Real: insertadas={resultado2.PolizasInsertadas}, actualizadas={resultado2.PolizasActualizadas}");

var polizaActualizada = store.Polizas["POL-001-2026"];
Assert(polizaActualizada.MontoCobertura == 40000000.00m, $"El monto de cobertura se reemplazó a 40,000,000. Real: {polizaActualizada.MontoCobertura}");
Assert(polizaActualizada.Asegurados.Single().Beneficiarios.Count == 3, $"Ahora tiene 3 beneficiarios (se añadió uno). Real: {polizaActualizada.Asegurados.Single().Beneficiarios.Count}");
Assert(polizaActualizada.Asegurados.Single().SumaPorcentajesBeneficiarios == 100.00m, "Los 3 beneficiarios siguen sumando 100%.");
Assert(polizaActualizada.VersionActualizacion == 2, $"El número de versión de la póliza avanzó a 2. Real: {polizaActualizada.VersionActualizacion}");
Assert(polizaActualizada.LoteCargaOrigenId == lote1.Id, "Se conserva cuál fue el lote de ORIGEN de la póliza (carga 1).");
Assert(polizaActualizada.LoteCargaUltimaActualizacionId == lote2.Id, "Se actualiza cuál fue el lote de la ÚLTIMA modificación (carga 2).");

Console.WriteLine();
Console.WriteLine("=====================================================================");
if (fallos.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(" TODAS LAS VERIFICACIONES PASARON ✔");
    Console.ResetColor();
    return 0;
}

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine($" {fallos.Count} VERIFICACIÓN(ES) FALLARON:");
foreach (var f in fallos) Console.WriteLine($"  - {f}");
Console.ResetColor();
return 1;