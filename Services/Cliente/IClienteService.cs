using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Cliente;

namespace AgendaiFisio.Services.Cliente
{
    public class IClienteService
    {
        ClienteResponseDTO GetClienteById(Guid id);
        ClienteResponseDTO CreateCliente(ClienteCreateDTO clienteCreateDTO);
        ClienteResponseDTO UpdateCliente(Guid id, ClienteUpdateDTO clienteUpdateDTO)
    }
}