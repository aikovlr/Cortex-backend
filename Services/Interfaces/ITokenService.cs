using Cortex.Entities;

namespace Cortex.Services.Interfaces
{
    public interface ITokenService
    {
        string GerarToken(Usuario usuario, string? cargo = null, int? equipeId = null);
    }
}
