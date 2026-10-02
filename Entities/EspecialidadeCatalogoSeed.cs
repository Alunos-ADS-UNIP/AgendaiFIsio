using System;
using System.Collections.Generic;

namespace AgendaiFisio.Entities
{
    // Lista inicial aprovada do catálogo de especialidades (ver docs/especialidades.md).
    // Os IDs são fixos de propósito: a mesma carga nunca pode gerar um ID diferente em outro
    // ambiente, nem trocar o ID de um item já existente ao ser reexecutada.
    public static class EspecialidadeCatalogoSeed
    {
        public static readonly IReadOnlyList<Especialidade> Itens = new List<Especialidade>
        {
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000001"), Nome = "Acupuntura" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000002"), Nome = "Aquática" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000003"), Nome = "Cardiovascular" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000004"), Nome = "Dermatofuncional" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000005"), Nome = "Fisioterapia do Trabalho" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000006"), Nome = "Esportiva" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000007"), Nome = "Gerontologia" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000008"), Nome = "Neurofuncional" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000009"), Nome = "Oncologia" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000010"), Nome = "Osteopatia" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000011"), Nome = "Quiropraxia" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000012"), Nome = "Reumatologia" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000013"), Nome = "Fisioterapia Respiratória" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000014"), Nome = "Fisioterapia em Saúde da Mulher" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000015"), Nome = "Traumato-Ortopédica" },
            new Especialidade { Id = new Guid("00000000-0000-0000-0000-000000000016"), Nome = "Terapia Intensiva" },
        };
    }
}
