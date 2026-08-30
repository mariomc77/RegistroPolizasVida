using Microsoft.EntityFrameworkCore;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.DataAccess.Repositorios;

public sealed class PolizaRepository : IPolizaRepository
{
    private readonly RegistroPolizasVidaDbContext _db;

    public PolizaRepository(RegistroPolizasVidaDbContext db)
    {
        _db = db;
    }

    public Task<Poliza?> ObtenerPorNumeroAsync(
        string numeroPoliza,
        CancellationToken ct = default)
    {
        return _db.Polizas
            .Include(p => p.Asegurados)
                .ThenInclude(a => a.Beneficiarios)
            .FirstOrDefaultAsync(
                p => p.NumeroPoliza == numeroPoliza,
                ct);
    }

    public async Task<AccionPersistencia> GuardarAsync(
        Poliza poliza,
        Guid loteCargaId,
        CancellationToken ct = default)
    {
        var existente = await _db.Polizas
            .Include(p => p.Asegurados)
                .ThenInclude(a => a.Beneficiarios)
            .FirstOrDefaultAsync(
                p => p.NumeroPoliza == poliza.NumeroPoliza,
                ct);

        var ahora = DateTime.UtcNow;

        if (existente is null)
        {
            PrepararNuevaPoliza(poliza);

            poliza.LoteCargaOrigenId = loteCargaId;
            poliza.LoteCargaUltimaActualizacionId = loteCargaId;
            poliza.FechaCreacion = ahora;
            poliza.FechaActualizacion = ahora;
            poliza.VersionActualizacion = 1;

            await _db.Polizas.AddAsync(poliza, ct);

            return AccionPersistencia.Insertada;
        }

        existente.Tipo = poliza.Tipo;
        existente.FechaEmision = poliza.FechaEmision;
        existente.FechaVencimiento = poliza.FechaVencimiento;
        existente.MontoCobertura = poliza.MontoCobertura;
        existente.Moneda = poliza.Moneda;

        ActualizarTomador(
            existente.Tomador,
            poliza.Tomador);

        SincronizarAsegurados(
            existente,
            poliza);

        existente.LoteCargaUltimaActualizacionId =
            loteCargaId;

        existente.FechaActualizacion =
            ahora;

        existente.VersionActualizacion++;

        return AccionPersistencia.Actualizada;
    }

    private static void PrepararNuevaPoliza(
        Poliza poliza)
    {
        foreach (var asegurado in poliza.Asegurados)
        {
            asegurado.PolizaNumero =
                poliza.NumeroPoliza;

            asegurado.Poliza =
                poliza;

            foreach (var beneficiario
                     in asegurado.Beneficiarios)
            {
                beneficiario.AseguradoId =
                    asegurado.Id;

                beneficiario.Asegurado =
                    asegurado;
            }
        }
    }

    private static void ActualizarTomador(
        Tomador existente,
        Tomador nuevo)
    {
        existente.TipoPersona =
            nuevo.TipoPersona;

        existente.Cedula =
            nuevo.Cedula;

        existente.CedulaJuridica =
            nuevo.CedulaJuridica;

        existente.Nombre =
            nuevo.Nombre;

        existente.PrimerApellido =
            nuevo.PrimerApellido;

        existente.SegundoApellido =
            nuevo.SegundoApellido;

        existente.RazonSocial =
            nuevo.RazonSocial;

        existente.Telefono =
            nuevo.Telefono;

        existente.Correo =
            nuevo.Correo;

        existente.Direccion =
            nuevo.Direccion;
    }

    private void SincronizarAsegurados(
        Poliza existente,
        Poliza nueva)
    {
        var aseguradosActuales =
            existente.Asegurados.ToList();

        foreach (var aseguradoActual
                 in aseguradosActuales)
        {
            var sigueExistiendo =
                nueva.Asegurados.Any(
                    a => string.Equals(
                        a.Cedula,
                        aseguradoActual.Cedula,
                        StringComparison.OrdinalIgnoreCase));

            if (!sigueExistiendo)
            {
                existente.Asegurados.Remove(
                    aseguradoActual);

                _db.Asegurados.Remove(
                    aseguradoActual);
            }
        }

        foreach (var aseguradoNuevo
                 in nueva.Asegurados)
        {
            var aseguradoActual =
                existente.Asegurados.FirstOrDefault(
                    a => string.Equals(
                        a.Cedula,
                        aseguradoNuevo.Cedula,
                        StringComparison.OrdinalIgnoreCase));

            if (aseguradoActual is null)
            {
                aseguradoNuevo.PolizaNumero =
                    existente.NumeroPoliza;

                aseguradoNuevo.Poliza =
                    existente;

                foreach (var beneficiario
                         in aseguradoNuevo.Beneficiarios)
                {
                    beneficiario.AseguradoId =
                        aseguradoNuevo.Id;

                    beneficiario.Asegurado =
                        aseguradoNuevo;
                }

                existente.Asegurados.Add(
                    aseguradoNuevo);

                _db.Asegurados.Add(
                    aseguradoNuevo);

                continue;
            }

            aseguradoActual.Nombre =
                aseguradoNuevo.Nombre;

            aseguradoActual.PrimerApellido =
                aseguradoNuevo.PrimerApellido;

            aseguradoActual.SegundoApellido =
                aseguradoNuevo.SegundoApellido;

            aseguradoActual.FechaNacimiento =
                aseguradoNuevo.FechaNacimiento;

            aseguradoActual.Telefono =
                aseguradoNuevo.Telefono;

            aseguradoActual.Correo =
                aseguradoNuevo.Correo;

            SincronizarBeneficiarios(
                aseguradoActual,
                aseguradoNuevo);
        }
    }

    private void SincronizarBeneficiarios(
        Asegurado existente,
        Asegurado nuevo)
    {
        var beneficiariosActuales =
            existente.Beneficiarios.ToList();

        foreach (var beneficiarioActual
                 in beneficiariosActuales)
        {
            var sigueExistiendo =
                nuevo.Beneficiarios.Any(
                    b => string.Equals(
                        ClaveBeneficiario(b),
                        ClaveBeneficiario(beneficiarioActual),
                        StringComparison.OrdinalIgnoreCase));

            if (!sigueExistiendo)
            {
                existente.Beneficiarios.Remove(
                    beneficiarioActual);

                _db.Beneficiarios.Remove(
                    beneficiarioActual);
            }
        }

        foreach (var beneficiarioNuevo
                 in nuevo.Beneficiarios)
        {
            var beneficiarioActual =
                existente.Beneficiarios.FirstOrDefault(
                    b => string.Equals(
                        ClaveBeneficiario(b),
                        ClaveBeneficiario(beneficiarioNuevo),
                        StringComparison.OrdinalIgnoreCase));

            if (beneficiarioActual is null)
            {
                beneficiarioNuevo.AseguradoId =
                    existente.Id;

                beneficiarioNuevo.Asegurado =
                    existente;

                existente.Beneficiarios.Add(
                    beneficiarioNuevo);

                _db.Beneficiarios.Add(
                    beneficiarioNuevo);

                continue;
            }

            beneficiarioActual.TipoPersona =
                beneficiarioNuevo.TipoPersona;

            beneficiarioActual.Cedula =
                beneficiarioNuevo.Cedula;

            beneficiarioActual.CedulaJuridica =
                beneficiarioNuevo.CedulaJuridica;

            beneficiarioActual.Nombre =
                beneficiarioNuevo.Nombre;

            beneficiarioActual.PrimerApellido =
                beneficiarioNuevo.PrimerApellido;

            beneficiarioActual.SegundoApellido =
                beneficiarioNuevo.SegundoApellido;

            beneficiarioActual.RazonSocial =
                beneficiarioNuevo.RazonSocial;

            beneficiarioActual.PorcentajeBeneficio =
                beneficiarioNuevo.PorcentajeBeneficio;

            beneficiarioActual.Telefono =
                beneficiarioNuevo.Telefono;

            beneficiarioActual.Correo =
                beneficiarioNuevo.Correo;
        }
    }

    private static string ClaveBeneficiario(
        Beneficiario beneficiario)
    {
        var identificacion =
            beneficiario.TipoPersona ==
            TipoPersona.Juridica
                ? beneficiario.CedulaJuridica
                : beneficiario.Cedula;

        return
            $"{beneficiario.TipoPersona}|{identificacion}"
                .Trim()
                .ToUpperInvariant();
    }
}