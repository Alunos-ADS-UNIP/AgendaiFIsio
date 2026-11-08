# Diagrama entidade-relacionamento conceitual — AgendaiFisio

Este modelo representa os conceitos de negócio do MVP. Ele não descreve tipos
SQL, índices, triggers ou detalhes internos do Entity Framework.

```mermaid
erDiagram
    USUARIO ||--o| PACIENTE : "possui perfil"
    USUARIO ||--o| PROFISSIONAL : "possui perfil"

    PACIENTE ||--|| ENDERECO : "possui"
    PROFISSIONAL ||--|| ENDERECO : "atende em"
    ESPECIALIDADE o|--o{ PROFISSIONAL : "classifica"

    PACIENTE ||--o{ AGENDAMENTO : "solicita"
    PROFISSIONAL ||--o{ AGENDAMENTO : "atende"

    PROFISSIONAL ||--o{ BLOQUEIO_AGENDA : "define"

    PACIENTE ||--o{ AVALIACAO_FISIOTERAPEUTICA : "recebe"
    PROFISSIONAL ||--o{ AVALIACAO_FISIOTERAPEUTICA : "realiza"
    AVALIACAO_FISIOTERAPEUTICA ||--o| PLANO_TERAPEUTICO : "origina"

    USUARIO {
        identificador id
        email email
        credencial senha
        perfil tipoUsuario
        confirmacao aceiteLgpd
        data_hora aceiteLgpdEm
    }

    PACIENTE {
        identificador id
        texto nomeCompleto
        documento cpf
        contato telefone
        data dataNascimento
        texto sexo
        texto estadoCivil
    }

    ENDERECO {
        identificador id
        texto rua
        texto numero
        texto complemento
        texto cep
        texto bairro
        texto cidade
        texto estado
    }

    PROFISSIONAL {
        identificador id
        texto nomeCompleto
        documento cpf
        registro crefito
        contato telefone
        data dataNascimento
        texto bio
        imagem fotoPerfil
        data dataCadastro
        estado ativo
    }

    ESPECIALIDADE {
        identificador id
        texto nome
    }

    AGENDAMENTO {
        identificador id
        data_hora dataHora
        estado status
        texto observacoes
    }

    BLOQUEIO_AGENDA {
        identificador id
        data_hora inicio
        data_hora fim
        categoria tipo
        texto motivo
        data_hora criadoEm
    }

    AVALIACAO_FISIOTERAPEUTICA {
        identificador id
        texto queixaPrincipal
        texto historiaPregressa
        texto habitosDeVida
        texto tratamentosRealizados
        texto antecedentes
        data dataAvaliacao
    }

    PLANO_TERAPEUTICO {
        identificador id
        texto objetivo
        numero quantidadeSessoes
        texto procedimentos
    }
```

## Regras conceituais

- Cada usuário do MVP possui um único tipo de perfil: paciente **ou**
  profissional. Os dois perfis são mutuamente exclusivos.
- Cada paciente possui um endereço obrigatório.
- Cada profissional precisa informar um endereço de atendimento para concluir
  o perfil e aparecer com o local completo para os pacientes.
- Um profissional pode estar associado a uma especialidade do catálogo.
- Cada agendamento liga exatamente um paciente a exatamente um profissional.
- Cancelar um agendamento preserva a entidade e altera seu estado para
  `Cancelado`; o registro não é excluído.
- Um profissional pode criar vários bloqueios em sua agenda.
- Uma avaliação é realizada por um profissional para um paciente e pode
  originar, no máximo, um plano terapêutico.
- A imagem do profissional é armazenada externamente; o sistema mantém apenas
  a referência da foto no perfil profissional.

## Legenda de cardinalidade

- `||`: exatamente um.
- `o|`: zero ou um.
- `o{`: zero ou muitos.
