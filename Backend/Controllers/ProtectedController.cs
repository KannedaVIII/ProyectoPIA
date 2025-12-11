using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Este controlador requiere un JWT válido en cada petición
public class ProtectedController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // El acceso a este método solo es posible si se proporciona un JWT válido
        // Puedes acceder a la información del usuario desde User.Claims
        var username = User.Identity.Name;
        return Ok($"¡Hola, {username}! Acceso concedido al recurso protegido.");
    }
}