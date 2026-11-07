IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [Enderecos] (
        [Id] uniqueidentifier NOT NULL,
        [Rua] nvarchar(max) NOT NULL,
        [Numero] nvarchar(max) NOT NULL,
        [Complemento] nvarchar(max) NOT NULL,
        [Cep] nvarchar(max) NOT NULL,
        [Bairro] nvarchar(max) NOT NULL,
        [Cidade] nvarchar(max) NOT NULL,
        [Estado] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Enderecos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [Especialidades] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(100) COLLATE Latin1_General_CI_AI NOT NULL,
        CONSTRAINT [PK_Especialidades] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(450) NOT NULL,
        [SenhaHash] nvarchar(max) NOT NULL,
        [TipoUsuario] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [Pacientes] (
        [Id] uniqueidentifier NOT NULL,
        [NomeCompleto] nvarchar(max) NOT NULL,
        [Cpf] nvarchar(max) NOT NULL,
        [Telefone] nvarchar(max) NOT NULL,
        [DataNascimento] date NOT NULL,
        [Sexo] nvarchar(max) NOT NULL,
        [EstadoCivil] nvarchar(max) NOT NULL,
        [EnderecoId] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Pacientes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pacientes_Enderecos_EnderecoId] FOREIGN KEY ([EnderecoId]) REFERENCES [Enderecos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Pacientes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [Profissionais] (
        [Id] uniqueidentifier NOT NULL,
        [NomeCompleto] nvarchar(max) NOT NULL,
        [Cpf] nvarchar(max) NOT NULL,
        [Crefito] nvarchar(450) NOT NULL,
        [Telefone] nvarchar(max) NOT NULL,
        [DataNascimento] date NOT NULL,
        [Bio] nvarchar(max) NOT NULL,
        [DataCadastro] datetime2 NOT NULL,
        [Ativo] bit NOT NULL,
        [EspecialidadeId] uniqueidentifier NULL,
        [Especialidade] nvarchar(max) NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Profissionais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Profissionais_Especialidades_EspecialidadeId] FOREIGN KEY ([EspecialidadeId]) REFERENCES [Especialidades] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Profissionais_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [Agendamentos] (
        [Id] uniqueidentifier NOT NULL,
        [PacienteId] uniqueidentifier NOT NULL,
        [ProfissionalId] uniqueidentifier NOT NULL,
        [DataHora] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Observacoes] nvarchar(max) NULL,
        CONSTRAINT [PK_Agendamentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Agendamentos_Pacientes_PacienteId] FOREIGN KEY ([PacienteId]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Agendamentos_Profissionais_ProfissionalId] FOREIGN KEY ([ProfissionalId]) REFERENCES [Profissionais] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [AvaliacaoFisioterapeuta] (
        [Id] uniqueidentifier NOT NULL,
        [QueixaPrincipal] nvarchar(max) NOT NULL,
        [HistoriaPregressa] nvarchar(max) NOT NULL,
        [HabitosDeVida] nvarchar(max) NOT NULL,
        [TratamentosRealizados] nvarchar(max) NOT NULL,
        [AntecedentesPessoaisFamiliares] nvarchar(max) NOT NULL,
        [DataAvaliacao] datetime2 NOT NULL,
        [ProfissionalId] uniqueidentifier NOT NULL,
        [PacienteId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AvaliacaoFisioterapeuta] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AvaliacaoFisioterapeuta_Pacientes_PacienteId] FOREIGN KEY ([PacienteId]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AvaliacaoFisioterapeuta_Profissionais_ProfissionalId] FOREIGN KEY ([ProfissionalId]) REFERENCES [Profissionais] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [BloqueiosAgenda] (
        [Id] uniqueidentifier NOT NULL,
        [ProfissionalId] uniqueidentifier NOT NULL,
        [Inicio] datetime2 NOT NULL,
        [Fim] datetime2 NOT NULL,
        [Tipo] nvarchar(20) NOT NULL,
        [Motivo] nvarchar(500) NULL,
        [CriadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_BloqueiosAgenda] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BloqueiosAgenda_Intervalo] CHECK ([Fim] > [Inicio]),
        CONSTRAINT [FK_BloqueiosAgenda_Profissionais_ProfissionalId] FOREIGN KEY ([ProfissionalId]) REFERENCES [Profissionais] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE TABLE [PlanosTerapeutico] (
        [Id] uniqueidentifier NOT NULL,
        [Objetivo] nvarchar(max) NOT NULL,
        [QtdSessoes] int NOT NULL,
        [Procedimentos] nvarchar(max) NOT NULL,
        [AvaliacaoFisioterapeutaId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PlanosTerapeutico] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanosTerapeutico_AvaliacaoFisioterapeuta_AvaliacaoFisioterapeutaId] FOREIGN KEY ([AvaliacaoFisioterapeutaId]) REFERENCES [AvaliacaoFisioterapeuta] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Nome') AND [object_id] = OBJECT_ID(N'[Especialidades]'))
        SET IDENTITY_INSERT [Especialidades] ON;
    EXEC(N'INSERT INTO [Especialidades] ([Id], [Nome])
    VALUES (''00000000-0000-0000-0000-000000000001'', N''Acupuntura''),
    (''00000000-0000-0000-0000-000000000002'', N''Aquática''),
    (''00000000-0000-0000-0000-000000000003'', N''Cardiovascular''),
    (''00000000-0000-0000-0000-000000000004'', N''Dermatofuncional''),
    (''00000000-0000-0000-0000-000000000005'', N''Fisioterapia do Trabalho''),
    (''00000000-0000-0000-0000-000000000006'', N''Esportiva''),
    (''00000000-0000-0000-0000-000000000007'', N''Gerontologia''),
    (''00000000-0000-0000-0000-000000000008'', N''Neurofuncional''),
    (''00000000-0000-0000-0000-000000000009'', N''Oncologia''),
    (''00000000-0000-0000-0000-000000000010'', N''Osteopatia''),
    (''00000000-0000-0000-0000-000000000011'', N''Quiropraxia''),
    (''00000000-0000-0000-0000-000000000012'', N''Reumatologia''),
    (''00000000-0000-0000-0000-000000000013'', N''Fisioterapia Respiratória''),
    (''00000000-0000-0000-0000-000000000014'', N''Fisioterapia em Saúde da Mulher''),
    (''00000000-0000-0000-0000-000000000015'', N''Traumato-Ortopédica''),
    (''00000000-0000-0000-0000-000000000016'', N''Terapia Intensiva'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Nome') AND [object_id] = OBJECT_ID(N'[Especialidades]'))
        SET IDENTITY_INSERT [Especialidades] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Agendamentos_PacienteId_DataHora] ON [Agendamentos] ([PacienteId], [DataHora]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Agendamentos_ProfissionalId_DataHora] ON [Agendamentos] ([ProfissionalId], [DataHora]) WHERE [Status] <> ''Cancelado''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE INDEX [IX_AvaliacaoFisioterapeuta_PacienteId] ON [AvaliacaoFisioterapeuta] ([PacienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE INDEX [IX_AvaliacaoFisioterapeuta_ProfissionalId] ON [AvaliacaoFisioterapeuta] ([ProfissionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE INDEX [IX_BloqueiosAgenda_ProfissionalId_Inicio] ON [BloqueiosAgenda] ([ProfissionalId], [Inicio]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Especialidades_Nome] ON [Especialidades] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Pacientes_EnderecoId] ON [Pacientes] ([EnderecoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Pacientes_UsuarioId] ON [Pacientes] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlanosTerapeutico_AvaliacaoFisioterapeutaId] ON [PlanosTerapeutico] ([AvaliacaoFisioterapeutaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Profissionais_Crefito] ON [Profissionais] ([Crefito]) WHERE [Crefito] <> ''''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Profissionais_EspecialidadeId] ON [Profissionais] ([EspecialidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Profissionais_UsuarioId] ON [Profissionais] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE OR ALTER TRIGGER [TR_Agendamentos_ValidarBloqueio]
    ON [Agendamentos]
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        IF EXISTS (
            SELECT 1
            FROM inserted i
            INNER JOIN [BloqueiosAgenda] b WITH (UPDLOCK, HOLDLOCK)
                ON b.[ProfissionalId] = i.[ProfissionalId]
               AND b.[Inicio] <= i.[DataHora]
               AND b.[Fim] > i.[DataHora]
            WHERE i.[Status] <> 'Cancelado')
        BEGIN
            THROW 51001, 'O horário está bloqueado pelo profissional.', 1;
        END;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    CREATE OR ALTER TRIGGER [TR_BloqueiosAgenda_ValidarConsulta]
    ON [BloqueiosAgenda]
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        IF EXISTS (
            SELECT 1
            FROM inserted i
            INNER JOIN [Agendamentos] a WITH (UPDLOCK, HOLDLOCK)
                ON a.[ProfissionalId] = i.[ProfissionalId]
               AND a.[DataHora] >= i.[Inicio]
               AND a.[DataHora] < i.[Fim]
            WHERE a.[Status] <> 'Cancelado')
        BEGIN
            THROW 51002, 'O período possui uma consulta e não pode ser bloqueado.', 1;
        END;

        IF EXISTS (
            SELECT 1
            FROM inserted i
            INNER JOIN [BloqueiosAgenda] b WITH (UPDLOCK, HOLDLOCK)
                ON b.[Id] <> i.[Id]
               AND b.[ProfissionalId] = i.[ProfissionalId]
               AND b.[Inicio] < i.[Fim]
               AND b.[Fim] > i.[Inicio])
        BEGIN
            THROW 51003, 'O período informado já possui um bloqueio.', 1;
        END;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105235403_InicialSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105235403_InicialSqlServer', N'10.0.13');
END;

COMMIT;
GO
