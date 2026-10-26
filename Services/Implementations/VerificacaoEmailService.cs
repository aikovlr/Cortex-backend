using System.Security.Cryptography;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class VerificacaoEmailService : IVerificacaoEmailService
    {
        private const int CodigoExpiracaoMinutos = 15;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<VerificacaoEmailService> _logger;

        public VerificacaoEmailService(
            IUsuarioRepository usuarioRepository,
            ILogger<VerificacaoEmailService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task<string> GerarEArmazenarCodigoAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            // Gera código criptograficamente seguro de 6 dígitos (100000 a 999999)
            var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Armazena o hash do código (nunca o texto claro)
            usuario.CodigoVerificacaoHash = BCrypt.Net.BCrypt.HashPassword(codigo);
            usuario.CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(CodigoExpiracaoMinutos);

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            // Exibição em console para fins de desenvolvimento
            Console.WriteLine("\n=======================================================");
            Console.WriteLine("[DESENVOLVIMENTO] CÓDIGO DE VERIFICAÇÃO GERADO");
            Console.WriteLine($"UsuarioId: {usuarioId} | E-mail: {usuario.Email}");
            Console.WriteLine($"Código (6 dígitos): {codigo}");
            Console.WriteLine($"Tempo limite de expiração: {CodigoExpiracaoMinutos} minutos (expira em {usuario.CodigoVerificacaoExpiracao:yyyy-MM-dd HH:mm:ss} UTC)");
            Console.WriteLine("=======================================================\n");

            _logger.LogInformation(
                "Código de verificação gerado para UsuarioId {UsuarioId}. Expira em {Expiracao} UTC.",
                usuarioId, usuario.CodigoVerificacaoExpiracao);

            return codigo; // Retornado apenas para envio via gateway de e-mail e ambiente de desenvolvimento
        }

        public async Task<bool> VerificarCodigoAsync(string email, string codigoRecebido)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (string.IsNullOrWhiteSpace(usuario.CodigoVerificacaoHash) ||
                !usuario.CodigoVerificacaoExpiracao.HasValue)
                throw new InvalidOperationException("Nenhum código de verificação foi solicitado para este e-mail.");

            if (DateTime.UtcNow > usuario.CodigoVerificacaoExpiracao.Value)
                throw new InvalidOperationException("O código de verificação expirou. Solicite um novo código.");

            var codigoValido = BCrypt.Net.BCrypt.Verify(codigoRecebido, usuario.CodigoVerificacaoHash);
            if (!codigoValido)
                throw new ArgumentException("Código de verificação inválido.");

            // Invalida o código após uso (uso único)
            usuario.EmailVerificado = true;
            usuario.CodigoVerificacaoHash = null;
            usuario.CodigoVerificacaoExpiracao = null;

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            _logger.LogInformation(
                "E-mail verificado com sucesso para UsuarioId {UsuarioId}.", usuario.Id);

            return true;
        }
    }
}
