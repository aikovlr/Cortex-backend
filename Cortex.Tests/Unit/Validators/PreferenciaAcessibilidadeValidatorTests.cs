using Cortex.DTOs.Request;
using Cortex.DTOs.Validators;
using Xunit;

namespace Cortex.Tests.Unit.Validators
{
    public class PreferenciaAcessibilidadeValidatorTests
    {
        private readonly PreferenciaAcessibilidadeValidator _validator = new();

        [Theory]
        [InlineData(12, true)]
        [InlineData(14, true)]
        [InlineData(16, true)]
        [InlineData(18, true)]
        [InlineData(20, true)]
        [InlineData(22, true)]
        [InlineData(24, true)]
        [InlineData(10, false)] // Menor que o mínimo de 12px
        [InlineData(11, false)] // Ímpar e menor que 12px
        [InlineData(13, false)] // Ímpar
        [InlineData(15, false)] // Ímpar
        [InlineData(17, false)] // Ímpar
        [InlineData(19, false)] // Ímpar
        [InlineData(21, false)] // Ímpar
        [InlineData(23, false)] // Ímpar
        [InlineData(25, false)] // Maior que 24px e ímpar
        [InlineData(26, false)] // Maior que 24px
        public void ValidarTamanhoFonte_Range12A24_SomenteNumerosPares(int tamanhoFonte, bool esperadoValido)
        {
            var dto = new AtualizarPreferenciaAcessibilidadeDTO
            {
                TamanhoFonte = tamanhoFonte
            };

            var resultado = _validator.Validate(dto);

            Assert.Equal(esperadoValido, resultado.IsValid);
        }

        [Theory]
        [InlineData("light", true)]
        [InlineData("dark", true)]
        [InlineData("LIGHT", true)]
        [InlineData("DARK", true)]
        [InlineData("blue", false)]
        [InlineData("custom", false)]
        public void ValidarTema_ApenasLightEDark(string tema, bool esperadoValido)
        {
            var dto = new AtualizarPreferenciaAcessibilidadeDTO
            {
                Tema = tema
            };

            var resultado = _validator.Validate(dto);

            Assert.Equal(esperadoValido, resultado.IsValid);
        }
    }
}

