using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Interfaces;

/// <summary>
/// Acceso a pólizas, con soporte de "upsert" (insertar o reemplazar) por número de
/// póliza: es la operación clave para el escenario de actualización de lotes
/// (ver polizas_lote_05.xml, que reemplaza la póliza cargada en polizas_lote_01.xml).
/// </summary>
public interface IPolizaRepository
{
    Task<Poliza?> ObtenerPorNumeroAsync(string numeroPoliza, CancellationToken ct = default);

    /// <summary>
    /// Inserta la póliza si no existe, o reemplaza sus datos (tomador, asegurados,
    /// beneficiarios) si ya existía. Devuelve si fue una inserción o una actualización.
    /// </summary>
    Task<AccionPersistencia> GuardarAsync(Poliza poliza, Guid loteCargaId, CancellationToken ct = default);
}

/// <summary>Acceso a los registros de seguimiento de lotes (LoteCarga / ArchivoLote / ErrorProcesamiento).</summary>
public interface ILoteCargaRepository
{
    Task AgregarAsync(LoteCarga lote, CancellationToken ct = default);
    Task<LoteCarga?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LoteCarga>> ObtenerRecientesAsync(int cantidad = 20, CancellationToken ct = default);
    Task ActualizarAsync(LoteCarga lote, CancellationToken ct = default);

    /// <summary>Registra explícitamente como NUEVOS los ArchivoLote (y sus ErrorProcesamiento), para que EF Core genere INSERT en vez de UPDATE.</summary>
    Task AgregarArchivosAsync(IEnumerable<ArchivoLote> archivos, CancellationToken ct = default);
}

/// <summary>
/// Unidad de trabajo: agrupa los repositorios y expone <see cref="GuardarCambiosAsync"/>
/// para confirmar todos los cambios de una operación en una sola transacción.
/// Cada instancia envuelve su propio contexto de base de datos (no es hilo-seguro);
/// use <see cref="IUnitOfWorkFactory"/> para obtener una instancia nueva por tarea
/// cuando se procesa en paralelo.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IPolizaRepository Polizas { get; }
    ILoteCargaRepository Lotes { get; }
    Task<int> GuardarCambiosAsync(CancellationToken ct = default);
}

/// <summary>
/// Crea instancias de <see cref="IUnitOfWork"/> bajo demanda. Existe porque el
/// contexto de EF Core que hay detrás no admite uso concurrente: cada tarea que
/// corre en paralelo (por ejemplo, cada archivo .XML de un lote) debe pedir la suya.
/// </summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Crear();
}