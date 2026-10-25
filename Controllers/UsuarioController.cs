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
    public class UsuarioController : BaseController
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IEquipeAuthorizationService _authService;
        private readonly IVerificacaoEmailService _verificacaoEmailService;
        private readonly IEmailGateway _emailGateway;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(
            IUsuarioService usuarioService,
            IEquipeAuthorizationService authService,
            IVerificacaoEmailService verificacaoEmailService,
            IEmailGateway emailGateway,
            ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _authService = authService;
            _verificacaoEmailService = verificacaoEmailService;
            _emailGateway = emailGateway;
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
        [Authorize]
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
                return NaoEncontrado($"Nenhum usuário foi encontrado com o ID {id}.", "Usuário não encontrado");
            }

            return Ok(usuario);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Atualizar(int id, [FromForm] AtualizarUsuarioDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var ehAdminOuDono = await _authService.EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(currentUserId, id);

            if (currentUserId != id && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para atualizar os dados de outro usuário.");
            }

            var usuarioAtualizado = await _usuarioService.AtualizarUsuarioAsync(id, dto);

            _logger.LogInformation("Usuário atualizado: UserId {UserId} por SolicitanteId {SolicitanteId}", 
                id, currentUserId);

            return Ok(usuarioAtualizado);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> ObterMeuPerfil()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var usuario = await _usuarioService.ObterUsuarioPorIdAsync(currentUserId);
            if (usuario == null)
            {
                return NaoEncontrado($"Usuário autenticado não encontrado.", "Usuário não encontrado");
            }

            return Ok(usuario);
        }

        [HttpPut("me")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AtualizarMeuPerfil([FromForm] AtualizarUsuarioDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var usuarioAtualizado = await _usuarioService.AtualizarUsuarioAsync(currentUserId, dto);

            _logger.LogInformation("Perfil do próprio usuário atualizado: UserId {UserId}", currentUserId);

            return Ok(usuarioAtualizado);
        }

        [HttpDelete("minha-conta")]
        [Authorize]
        public async Task<IActionResult> ExcluirMinhaConta()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var deletado = await _usuarioService.DeletarUsuarioAsync(currentUserId, currentUserId);
            if (!deletado)
            {
                return NaoEncontrado($"Usuário autenticado não encontrado.", "Usuário não encontrado");
            }

            _logger.LogWarning("Auditoria: Auto-exclusão de conta de usuário. Id: {Id}", currentUserId);

            return NoContent();
        }

        [HttpPost("solicitar-verificacao-email")]
        [Authorize]
        public async Task<IActionResult> SolicitarVerificacaoEmail()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var usuario = await _usuarioService.ObterUsuarioPorIdAsync(currentUserId);
            if (usuario == null)
            {
                return NaoEncontrado("Usuário não encontrado.");
            }

            if (usuario.EmailVerificado)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "E-mail já verificado",
                    Detail = "O seu e-mail já se encontra verificado."
                });
            }

            var codigo = await _verificacaoEmailService.GerarEArmazenarCodigoAsync(currentUserId);
            await _emailGateway.EnviarEmailAsync(usuario.Email, "Verificação de e-mail", $"Seu código de verificação: {codigo}");

            _logger.LogInformation("Código de verificação de e-mail enviado para o usuário {UserId}", currentUserId);

            return Ok(new { message = "Código de verificação enviado para o seu e-mail." });
        }

        [HttpPost("verificar-email")]
        [Authorize]
        public async Task<IActionResult> ConfirmarVerificacaoEmail([FromBody] ConfirmarCodigoDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var usuario = await _usuarioService.ObterUsuarioPorIdAsync(currentUserId);
            if (usuario == null)
            {
                return NaoEncontrado("Usuário não encontrado.");
            }

            await _verificacaoEmailService.VerificarCodigoAsync(usuario.Email, dto.Codigo);

            _logger.LogInformation("E-mail verificado com sucesso para o usuário {UserId}", currentUserId);

            return Ok(new { message = "E-mail verificado com sucesso." });
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var ehDono = await _authService.EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(currentUserId, id);

            if (currentUserId != id && !ehDono)
            {
                throw new ForbiddenException("Você não possui permissão para excluir a conta de outro usuário.");
            }

            var deletado = await _usuarioService.DeletarUsuarioAsync(id, currentUserId);
            if (!deletado)
            {
                return NaoEncontrado($"Nenhum usuário foi encontrado com o ID {id}.", "Usuário não encontrado");
            }

            _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}", 
                "Usuario", id, currentUserId);

            return NoContent();
        }
    }
}
