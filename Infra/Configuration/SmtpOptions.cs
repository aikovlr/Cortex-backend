using System.ComponentModel.DataAnnotations;

namespace Cortex.Infra.Configuration
{
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public bool Enabled { get; set; } = false;

        public string Host { get; set; } = "localhost";

        [Range(1, 65535, ErrorMessage = "A porta SMTP deve estar entre 1 e 65535.")]
        public int Port { get; set; } = 1025;

        public bool UseSsl { get; set; } = false;

        public string? Username { get; set; }

        public string? Password { get; set; }

        [EmailAddress(ErrorMessage = "O endereço do remetente (FromAddress) deve ser um e-mail válido.")]
        public string FromAddress { get; set; } = "nao-responda@cortex.com";

        public string FromName { get; set; } = "Cortex";

        [Range(1, 120, ErrorMessage = "O timeout SMTP deve estar entre 1 e 120 segundos.")]
        public int TimeoutSeconds { get; set; } = 10;
    }
}

