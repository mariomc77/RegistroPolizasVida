using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.Application.Services;

/// <summary>
/// Orquesta el procesamiento completo de un lote: extraer el .ZIP, validar y
/// procesar cada .XML en paralelo, persistir las pólizas válidas y notificar el
/// resultado. Es el corazón del reto.
/// </summary>
public interface IBatchProcessingService
{
    /// <summary>
    /// Procesa el lote identificado por <paramref name="loteCargaId"/> (ya debe existir
    /// en estado Pendiente). Pensado para ejecutarse dentro del worker de la cola de
    /// tareas en segundo plano, nunca directamente en el hilo de la petición HTTP.
    /// </summary>
    Task<LoteCarga> ProcesarLoteAsync(
        Guid loteCargaId,
        byte[] contenidoZip,
        CancellationToken ct = default);
}