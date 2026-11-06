using System.Text.RegularExpressions;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public static class CustomValidationExtensions
    {
        // Regex robusto para validação de e-mail com TLD real (pelo menos 2 letras, ex: .com, .com.br, .io, etc.)
        // Evita aceitar "teste@algo" sem domínio válido ou sem extensão
        private static readonly Regex EmailStrictRegex = new(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // Regex para validar número de telefone internacional formatado ou somente dígitos
        // Permite prefixo '+' opcional, código do país, DDD, separadores ' ', '-', '(', ')' e dígitos.
        // Mínimo de 8 dígitos numéricos no total, máximo 20 caracteres totais.
        private static readonly Regex PhoneDigitsOnlyRegex = new(
            @"^\+?[0-9\s\-\(\)\.]{8,20}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static IRuleBuilderOptions<T, string?> ValidarEmailEstrito<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .MaximumLength(255).WithMessage("O e-mail não pode exceder 255 caracteres.")
                .Must(email => !string.IsNullOrWhiteSpace(email) && EmailStrictRegex.IsMatch(email.Trim()))
                .WithMessage("Formato de e-mail inválido. Informe um domínio completo (ex: nome@empresa.com).");
        }

        public static IRuleBuilderOptions<T, string?> ValidarTelefoneOpcional<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .MaximumLength(30).WithMessage("O telefone não pode exceder 30 caracteres.")
                .Must(tel =>
                {
                    if (string.IsNullOrWhiteSpace(tel)) return true;
                    // Deve conter apenas dígitos e símbolos aceitos de máscara (+, -, (, ), espaços)
                    if (!PhoneDigitsOnlyRegex.IsMatch(tel.Trim())) return false;
                    // Conta quantos dígitos reais existem
                    var digitCount = tel.Count(char.IsDigit);
                    return digitCount >= 8 && digitCount <= 15;
                })
                .WithMessage("O telefone deve conter apenas números válidos (entre 8 e 15 dígitos com DDI/DDD).");
        }
    }
}
