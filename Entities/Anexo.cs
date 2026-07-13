namespace Cortex.Entities
{
    public class Anexo
    {
        public int Id { get; set; }
        public required string UrlCaminho { get; set; }
        public required string NomeOriginal { get; set; }
        public required string MimeType { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
        public TipoEntidade TipoEntidade { get; set; } // Enum para identificar o tipo de entidade associada ao anexo
        public int? UsuarioId { get; set; }
        public int? TarefaId { get; set; }
        public int? RespostaTarefaId { get; set; }
        public Usuario? Usuario { get; set; }
        public Tarefa? Tarefa { get; set; }
        public RespostaTarefa? RespostaTarefa { get; set; }
    }
}