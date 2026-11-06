namespace Cortex.DTOs
{
    public class EmailMensagem
    {
        public required string Destinatario { get; set; }
        public string? NomeDestinatario { get; set; }
        public required string Assunto { get; set; }
        public required string CorpoHtml { get; set; }
        public string? CorpoTexto { get; set; }
    }
}

