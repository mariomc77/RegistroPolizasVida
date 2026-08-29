using System.IO.Compression;

namespace RegistroPolizasVida.Application.Zip;

public sealed class ZipBatchExtractor : IZipBatchExtractor
{
    public IReadOnlyList<EntradaZip> ExtraerArchivosXml(Stream contenidoZip)
    {
        using var archivo = new ZipArchive(contenidoZip, ZipArchiveMode.Read, leaveOpen: true);

        var resultado = new List<EntradaZip>();
        foreach (var entrada in archivo.Entries)
        {
            // Se ignoran carpetas (entradas de tamaño 0 que terminan en '/') y cualquier
            // archivo que no sea .xml: el .ZIP solo debe traer los lotes, nada más.
            if (string.IsNullOrEmpty(entrada.Name)) continue;
            if (!entrada.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;

            using var flujoEntrada = entrada.Open();
            using var memoria = new MemoryStream();
            flujoEntrada.CopyTo(memoria);

            resultado.Add(new EntradaZip(entrada.Name, memoria.ToArray()));
        }

        return resultado;
    }
}