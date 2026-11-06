using Cortex.DTOs.Request;
using Cortex.DTOs.Validators;
using Cortex.Entities;
using Xunit;

namespace Cortex.Tests.Unit.Validators
{
    public class ValidatorTests
    {
        [Theory]
        [InlineData("SenhaForte123!@", true)] // Válida
        [InlineData("fraca", false)]          // Sem maiúscula, número, especial, < 8 chars
        [InlineData("SenhaSemEspecial1", false)] // Sem especial
        [InlineData("senhasóminúscula123!", false)] // Sem maiúscula
        [InlineData("SENHASÓMAIÚSCULA123!", false)] // Sem minúscula
        [InlineData("SenhaSemNumero!", false)] // Sem número
        public void ValidarSenha_RN02_Complexidade(string senha, bool esperadaValida)
        {
            // Testa usando o validador de CriarUsuario
            var userValidator = new CriarUsuarioValidator();
            var dto = new CriarUsuarioDTO
            {
                Nome = "Teste",
                Email = "teste@exemplo.com",
                Senha = senha
            };

            var resultado = userValidator.Validate(dto);
            Assert.Equal(esperadaValida, resultado.IsValid);
        }

        [Fact]
        public void CriarMeta_RN15_DeveFalharSeDataSuperiorA12Meses()
        {
            var validator = new CriarMetaValidator();
            var dto = new CriarMetaDTO
            {
                Nome = "Meta Longa",
                TarefaId = 1,
                DataFechamento = DateTime.UtcNow.AddMonths(13) // > 12 meses
            };

            var resultado = validator.Validate(dto);
            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == "DataFechamento");
        }

        [Fact]
        public void CriarMeta_RN07_DeveFalharSeTarefaInvalida()
        {
            var validator = new CriarMetaValidator();
            var dto = new CriarMetaDTO
            {
                Nome = "Meta Sem Tarefa",
                TarefaId = 0, // Inválida
                DataFechamento = DateTime.UtcNow.AddMonths(6)
            };

            var resultado = validator.Validate(dto);
            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == "TarefaId");
        }

        [Theory]
        [InlineData(0, false)]    // Menor que 1h
        [InlineData(1, true)]     // 1h
        [InlineData(24, true)]    // 24h
        [InlineData(168, true)]   // 7 dias
        [InlineData(169, false)]  // Maior que 7 dias
        public void PreferenciaNotificacao_RN11_JanelaAntecedencia(int horas, bool esperadaValida)
        {
            var validator = new PreferenciaNotificacaoValidator();
            var dto = new ConfigurarPreferenciaDTO
            {
                Canal = CanalNotificacao.Email,
                AntecedenciaHoras = horas
            };

            var resultado = validator.Validate(dto);
            Assert.Equal(esperadaValida, resultado.IsValid);
        }

        [Fact]
        public void CriarTarefa_DeveFalharSeDataNoPassado()
        {
            var validator = new CriarTarefaValidator();
            var dto = new CriarTarefaDTO
            {
                Titulo = "Tarefa Passada",
                DataVencimento = DateTime.UtcNow.AddDays(-1),
                Prioridade = Prioridade.Media
            };

            var resultado = validator.Validate(dto);
            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == "DataVencimento");
        }

        [Theory]
        [InlineData("usuario@dominio.com", true)]
        [InlineData("usuario.sobrenome@empresa.com.br", true)]
        [InlineData("dev@cortex.io", true)]
        [InlineData("invalido@", false)]
        [InlineData("invalido@dominio", false)]
        [InlineData("invalido@dominio.", false)]
        [InlineData("invalido@.com", false)]
        [InlineData("@dominio.com", false)]
        [InlineData("sem_arroba.com", false)]
        public void CriarUsuario_ValidacaoEmailEstrito(string email, bool esperadaValida)
        {
            var validator = new CriarUsuarioValidator();
            var dto = new CriarUsuarioDTO
            {
                Nome = "Teste",
                Email = email,
                Senha = "SenhaForte123!@"
            };

            var resultado = validator.Validate(dto);
            Assert.Equal(esperadaValida, resultado.IsValid);
        }

        [Theory]
        [InlineData("+55 (11) 98765-4321", true)]
        [InlineData("11987654321", true)]
        [InlineData("+1 202-555-0143", true)]
        [InlineData("abc-def", false)]
        [InlineData("123", false)] // Menos de 8 dígitos
        [InlineData("123456789012345678", false)] // Mais de 15 dígitos
        public void CriarUsuario_ValidacaoTelefone(string telefone, bool esperadaValida)
        {
            var validator = new CriarUsuarioValidator();
            var dto = new CriarUsuarioDTO
            {
                Nome = "Teste",
                Email = "teste@exemplo.com",
                Senha = "SenhaForte123!@",
                Telefone = telefone
            };

            var resultado = validator.Validate(dto);
            Assert.Equal(esperadaValida, resultado.IsValid);
        }
    }
}
