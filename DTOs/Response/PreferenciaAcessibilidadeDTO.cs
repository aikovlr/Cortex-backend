namespace Cortex.DTOs.Response
{
    public class PreferenciaAcessibilidadeDTO
    {
        public string Tema { get; set; } = "light";
        public bool AltoContraste { get; set; } = false;
        public int TamanhoFonte { get; set; } = 16;
    }
}

