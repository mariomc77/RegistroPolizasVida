<<<<<<< HEAD
﻿using RegistroPolizasVida.Domain.Common;
=======
using RegistroPolizasVida.Domain.Common;
>>>>>>> 3f682a31288e87058c5d07272c55accfc734d393

namespace RegistroPolizasVida.Application.Validation;

public interface IXmlXsdValidator
{
    ResultadoValidacion Validar(byte[] contenidoXml, byte[] contenidoXsd);
}