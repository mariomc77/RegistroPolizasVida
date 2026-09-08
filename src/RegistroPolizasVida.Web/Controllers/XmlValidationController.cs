using Microsoft.AspNetCore.Mvc;
using RegistroPolizasVida.Application.Dtos;
using RegistroPolizasVida.Application.Validation;
using RegistroPolizasVida.Domain.Common;

namespace RegistroPolizasVida.Web.Controllers;

[ApiController]
[Route("api/xml-validation")]
public sealed class XmlValidationController : ControllerBase
{
    private readonly IXmlXsdValidator _validator;

    public XmlValidationController(IXmlXsdValidator validator)
    {
        _validator = validator;
    }

    [HttpPost("validate")]
    public ActionResult<ResultadoValidacion> Validar(
        [FromBody] ValidarXmlXsdRequest request)
    {
        var resultado = _validator.Validar(
            request.Xml,
            request.Xsd
        );

        return Ok(resultado);
    }
}