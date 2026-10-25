using Cortex.Services.Implementations;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _service;

        public PasswordServiceTests()
        {
            _service = new PasswordService();
        }

        [Fact]
        public void HashPassword_E_VerifyPassword_ComSucesso()
        {
            var senha = "SenhaForte123!@";
            var hash = _service.HashPassword(senha);

            Assert.NotNull(hash);
            Assert.True(_service.VerifyPassword(senha, hash));
            Assert.False(_service.VerifyPassword("SenhaErrada123!", hash));
        }

        [Theory]
        [InlineData("", "A senha é obrigatória.")]
        [InlineData("Curto1!", "A senha deve ter no mínimo 8 caracteres.")]
        [InlineData("semmaiuscula1!", "A senha deve conter pelo menos uma letra maiúscula.")]
        [InlineData("SEMMINUSCULA1!", "A senha deve conter pelo menos uma letra minúscula.")]
        [InlineData("SemNumero!@", "A senha deve conter pelo menos um número.")]
        [InlineData("SemCaractereEspecial1", "A senha deve conter pelo menos um caractere especial.")]
        public void ValidatePassword_MensagensEmPortugues(string senha, string mensagemEsperada)
        {
            var (isValid, errorMessage) = _service.ValidatePassword(senha);

            Assert.False(isValid);
            Assert.Equal(mensagemEsperada, errorMessage);
        }

        [Fact]
        public void ValidatePassword_SenhaValida_RetornaTrue()
        {
            var (isValid, errorMessage) = _service.ValidatePassword("SenhaValida123#");

            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
    }
}
