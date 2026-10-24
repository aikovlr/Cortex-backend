using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.DTOs.Request;
using Cortex.Infra.Exceptions;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(IUsuarioService usuarioService, ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Criar([FromForm] CriarUsuarioDTO dto)
        {
            var usuario = await _usuarioService.CriarUsuarioAsync(dto);

            _logger.LogInformation("Novo usuário registrado com sucesso: UserId {UserId}, Email {Email}", 
                usuario.Id, usuario.Email);

            return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, usuario);
        }

        [HttpGet]
        [Authorize(Roles = "Dono,Admin")]
        public async Task<IActionResult> Listar()
        {
            var usuarios = await _usuarioService.ListarUsuariosAsync();
            return Ok(usuarios);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var usuario = await _usuarioService.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Usuário não encontrado",
                    Detail = $"Nenhum usuário foi encontrado com o ID {id}.",
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(usuario);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Atualizar(int id, [FromForm] AtualizarUsuarioDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var ehAdminOuDono = User.IsInRole("Admin") || User.IsInRole("Dono");

            if (currentUserId != id && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para atualizar os dados de outro usuário.");
            }

            var usuarioAtualizado = await _usuarioService.AtualizarUsuarioAsync(id, dto);

            _logger.LogInformation("Usuário atualizado: UserId {UserId} por SolicitanteId {SolicitanteId}", 
                id, currentUserId);

            return Ok(usuarioAtualizado);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var ehDono = User.IsInRole("Dono");

            if (currentUserId != id && !ehDono)
            {
                throw new ForbiddenException("Você não possui permissão para excluir a conta de outro usuário.");
            }

            var deletado = await _usuarioService.DeletarUsuarioAsync(id);
            if (!deletado)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Usuário não encontrado",
                    Detail = $"Nenhum usuário foi encontrado com o ID {id}.",
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}", 
                "Usuario", id, currentUserId);

            return NoContent();
        }

        private int ObterUsuarioIdAutenticado()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(claim, out var id))
                return id;

            throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido no token.");
        }
    }
}
