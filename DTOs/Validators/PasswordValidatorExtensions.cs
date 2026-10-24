using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public static class PasswordValidatorExtensions
    {
        private const string SpecialCharacters = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        public static IRuleBuilderOptions<T, string> ValidarComplexidadeSenha<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("A senha é obrigatória.")
                .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
                .Must(p => p.Any(char.IsUpper)).WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
                .Must(p => p.Any(char.IsLower)).WithMessage("A senha deve conter pelo menos uma letra minúscula.")
                .Must(p => p.Any(char.IsDigit)).WithMessage("A senha deve conter pelo menos um número.")
                .Must(p => p.Any(c => SpecialCharacters.Contains(c))).WithMessage("A senha deve conter pelo menos um caractere especial.");
        }
    }
}
