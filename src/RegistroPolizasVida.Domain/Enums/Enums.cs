namespace RegistroPolizasVida.Domain.Enums;

/// <summary>Tipo de póliza según cantidad de asegurados.</summary>
public enum TipoPoliza
{
    Individual = 1,
    Colectiva = 2
}

/// <summary>Tipo de persona: física o jurídica. Aplica a Tomador y Beneficiario.</summary>
public enum TipoPersona
{
    Fisica = 1,
    Juridica = 2
}

/// <summary>Estado del proceso de un lote (archivo .ZIP cargado por el usuario).</summary>
public enum EstadoLote
{
    Pendiente = 1,
    Procesando = 2,
    Completado = 3,
    CompletadoConErrores = 4,
    Fallido = 5
}

/// <summary>Estado del procesamiento de un archivo .XML individual dentro de un lote.</summary>
public enum EstadoArchivo
{
    Pendiente = 1,
    Valido = 2,
    InvalidoEsquema = 3,
    ErrorNegocio = 4,
    Procesado = 5,
    ProcesadoConErrores = 6
}

/// <summary>Clasifica el origen de un error de procesamiento, para reportarlo con claridad.</summary>
public enum TipoError
{
    EsquemaXsd = 1,
    ReglaNegocio = 2,
    Persistencia = 3,
    Formato = 4
}

/// <summary>Acción resultante al persistir una póliza: nueva o actualización de una existente.</summary>
public enum AccionPersistencia
{
    Insertada = 1,
    Actualizada = 2
}
