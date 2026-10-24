using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class MembroEquipeService : IMembroEquipeService
    {
        private readonly IMembroEquipeRepository _membroEquipeRepository;
        private readonly IEquipeRepository _equipeRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public MembroEquipeService(
            IMembroEquipeRepository membroEquipeRepository,
            IEquipeRepository equipeRepository,
            IUsuarioRepository usuarioRepository)
        {
            _membroEquipeRepository = membroEquipeRepository;
            _equipeRepository = equipeRepository;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<ConsultaMembroEquipeDTO> AdicionarMembroAsync(CriarMembroEquipeDTO dto, int solicitanteId)
        {
            var equipeExiste = await _equipeRepository.EquipeExisteAsync(dto.EquipeId);
            if (!equipeExiste)
                throw new KeyNotFoundException("Equipe não encontrada.");

            var usuarioExiste = await _usuarioRepository.UsuarioExisteAsync(dto.UsuarioId);
            if (!usuarioExiste)
                throw new KeyNotFoundException("Usuário a ser adicionado não foi encontrado.");

            // RN04: Somente Dono e Admin da equipe podem adicionar membros
            var solicitanteMembro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(dto.EquipeId, solicitanteId);
            if (solicitanteMembro == null || (solicitanteMembro.Cargo != Cargo.Dono && solicitanteMembro.Cargo != Cargo.Admin))
            {
                throw new ForbiddenException("Apenas o Dono ou Administrador da equipe podem adicionar membros.");
            }

            // Administrador não pode adicionar novos membros como Dono ou Admin
            if (solicitanteMembro.Cargo == Cargo.Admin && dto.Cargo != Cargo.Membro)
            {
                throw new ForbiddenException("Administradores só podem adicionar novos usuários com o cargo de Membro.");
            }

            var jaEhMembro = await _membroEquipeRepository.MembroExisteNaEquipeAsync(dto.EquipeId, dto.UsuarioId);
            if (jaEhMembro)
            {
                throw new InvalidOperationException("O usuário já faz parte desta equipe.");
            }

            var novoMembro = new MembroEquipe
            {
                EquipeId = dto.EquipeId,
                UsuarioId = dto.UsuarioId,
                Cargo = dto.Cargo,
                DataEntrada = DateTime.UtcNow
            };

            await _membroEquipeRepository.AdicionarMembroEquipeAsync(novoMembro);

            var membroCompleto = await _membroEquipeRepository.ObterMembroEquipePorIdAsync(novoMembro.Id);
            return MapToConsultaDTO(membroCompleto ?? novoMembro);
        }

        public async Task<bool> RemoverMembroAsync(int membroId, int solicitanteId)
        {
            var membro = await _membroEquipeRepository.ObterMembroEquipePorIdAsync(membroId);
            if (membro == null)
                throw new KeyNotFoundException("Membro não encontrado.");

            // Se o próprio membro está saindo voluntariamente da equipe
            if (membro.UsuarioId == solicitanteId)
            {
                // Se for Dono, verificar se há outros donos na equipe
                if (membro.Cargo == Cargo.Dono)
                {
                    var membrosEquipe = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(membro.EquipeId);
                    var totalDonos = membrosEquipe.Count(m => m.Cargo == Cargo.Dono);
                    if (totalDonos <= 1)
                    {
                        throw new InvalidOperationException("O Dono não pode deixar a equipe sendo o único Dono. Promova outro membro a Dono primeiro.");
                    }
                }

                return await _membroEquipeRepository.RemoverMembroEquipeAsync(membroId);
            }

            // RN04: Somente Dono e Admin da equipe podem remover membros
            var solicitanteMembro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(membro.EquipeId, solicitanteId);
            if (solicitanteMembro == null || (solicitanteMembro.Cargo != Cargo.Dono && solicitanteMembro.Cargo != Cargo.Admin))
            {
                throw new ForbiddenException("Apenas o Dono ou Administrador da equipe possuem permissão para remover membros.");
            }

            // Admin não pode remover Dono nem outro Admin
            if (solicitanteMembro.Cargo == Cargo.Admin && (membro.Cargo == Cargo.Dono || membro.Cargo == Cargo.Admin))
            {
                throw new ForbiddenException("Administradores não podem remover o Dono ou outros Administradores da equipe.");
            }

            return await _membroEquipeRepository.RemoverMembroEquipeAsync(membroId);
        }

        public async Task<ConsultaMembroEquipeDTO> AlterarCargoAsync(int membroId, Cargo novoCargo, int solicitanteId)
        {
            var membro = await _membroEquipeRepository.ObterMembroEquipePorIdAsync(membroId);
            if (membro == null)
                throw new KeyNotFoundException("Membro não encontrado.");

            // RN03: Somente usuários com o cargo Dono podem alterar o cargo de outro usuário dentro da respectiva equipe
            var solicitanteMembro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(membro.EquipeId, solicitanteId);
            if (solicitanteMembro == null || solicitanteMembro.Cargo != Cargo.Dono)
            {
                throw new ForbiddenException("Apenas o Dono da equipe possui permissão para alterar cargos de membros.");
            }

            // Se o dono estiver rebaixando a si próprio, certificar que existe outro dono
            if (membro.UsuarioId == solicitanteId && novoCargo != Cargo.Dono)
            {
                var membrosEquipe = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(membro.EquipeId);
                var outrosDonos = membrosEquipe.Count(m => m.Cargo == Cargo.Dono && m.Id != membroId);
                if (outrosDonos == 0)
                {
                    throw new InvalidOperationException("A equipe deve possuir pelo menos um Dono. Defina outro membro como Dono antes de alterar seu próprio cargo.");
                }
            }

            membro.Cargo = novoCargo;
            await _membroEquipeRepository.AtualizarMembroEquipeAsync(membro);

            var membroAtualizado = await _membroEquipeRepository.ObterMembroEquipePorIdAsync(membroId);
            return MapToConsultaDTO(membroAtualizado ?? membro);
        }

        public async Task<IEnumerable<ConsultaMembroEquipeDTO>> ListarMembrosPorEquipeAsync(int equipeId, int? solicitanteId = null)
        {
            var equipeExiste = await _equipeRepository.EquipeExisteAsync(equipeId);
            if (!equipeExiste)
                throw new KeyNotFoundException("Equipe não encontrada.");

            if (solicitanteId.HasValue)
            {
                var ehMembro = await _membroEquipeRepository.MembroExisteNaEquipeAsync(equipeId, solicitanteId.Value);
                if (!ehMembro)
                    throw new ForbiddenException("Você não possui permissão para visualizar os membros desta equipe.");
            }

            var membros = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(equipeId);
            return membros.Select(MapToConsultaDTO);
        }

        private static ConsultaMembroEquipeDTO MapToConsultaDTO(MembroEquipe membro)
        {
            return new ConsultaMembroEquipeDTO
            {
                Id = membro.Id,
                DataEntrada = membro.DataEntrada,
                Cargo = membro.Cargo,
                UsuarioId = membro.UsuarioId,
                EquipeId = membro.EquipeId,
                Usuario = membro.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = membro.Usuario.Id,
                    Nome = membro.Usuario.Nome,
                    Email = membro.Usuario.Email,
                    Telefone = membro.Usuario.Telefone,
                    FotoPerfil = membro.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = membro.Usuario.FotoPerfil.Id,
                        UrlCaminho = membro.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = membro.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = membro.Usuario.FotoPerfil.MimeType,
                        DataEnvio = membro.Usuario.FotoPerfil.DataEnvio
                    } : null
                } : null
            };
        }
    }
}
