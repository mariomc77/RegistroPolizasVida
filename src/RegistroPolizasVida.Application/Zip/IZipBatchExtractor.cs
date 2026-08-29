namespace RegistroPolizasVida.Application.Zip;

/// <summary>Un archivo .XML extraído del .ZIP, listo para validar/parsear.</summary>
public sealed record EntradaZip(string NombreArchivo, byte[] Contenido);

/// <summary>
/// Abre un .ZIP subido por el usuario y expone sus entradas .XML como bytes en
/// memoria. Aislar esto detrás de una interfaz permite, por ejemplo, sustituirlo
/// en pruebas por una fuente en memoria sin tocar el resto del pipeline.
/// </summary>
public interface IZipBatchExtractor
{
    IReadOnlyList<EntradaZip> ExtraerArchivosXml(Stream contenidoZip);
}