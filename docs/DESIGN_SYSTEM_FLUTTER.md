# Design System Flutter - AgendaiFisio

## 1. Direcao visual

O AgendaiFisio deve transmitir **cuidado, movimento, confianca e simplicidade**. A interface sera baseada em Material 3, com bastante espaco em branco, cantos arredondados e hierarquia visual clara.

Diretrizes:

- Aparencia profissional sem parecer hospitalar ou fria.
- Verde-petroleo como identidade principal, associado a saude e recuperacao.
- Azul como cor secundaria para informacao e navegacao.
- Telas claras e limpas; modo escuro fica fora do escopo inicial.
- Textos e acoes em portugues do Brasil.
- Nenhum estado deve depender apenas de cor: usar sempre icone e texto.

## 2. Paleta de cores

### Marca e interface

| Token | Cor | Uso |
|---|---|---|
| `primary` | `#0F766E` | Botoes principais, links, item ativo e foco |
| `primaryDark` | `#115E59` | Estado pressionado e textos de destaque |
| `primaryLight` | `#CCFBF1` | Fundo selecionado e destaques suaves |
| `secondary` | `#2563EB` | Informacoes, filtros e acoes secundarias |
| `secondaryLight` | `#DBEAFE` | Fundo informativo |
| `background` | `#F8FAFC` | Fundo geral das telas |
| `surface` | `#FFFFFF` | Cards, formularios e modais |
| `surfaceVariant` | `#F1F5F9` | Campos desabilitados e secoes secundarias |
| `textPrimary` | `#0F172A` | Titulos e texto principal |
| `textSecondary` | `#475569` | Legendas e informacoes auxiliares |
| `textDisabled` | `#94A3B8` | Conteudo indisponivel |
| `border` | `#CBD5E1` | Bordas de campos e divisores |
| `divider` | `#E2E8F0` | Separadores leves |
| `error` | `#B91C1C` | Erros e acoes destrutivas |
| `errorLight` | `#FEE2E2` | Fundo de mensagens de erro |
| `success` | `#15803D` | Sucesso e confirmacao |
| `successLight` | `#DCFCE7` | Fundo de mensagens de sucesso |
| `warning` | `#B45309` | Alertas e pendencias |
| `warningLight` | `#FEF3C7` | Fundo de alertas |

### Status dos agendamentos

| Status | Fundo | Texto/icone | Icone sugerido |
|---|---|---|---|
| Agendado | `#DBEAFE` | `#1E40AF` | `calendar_month_outlined` |
| Confirmado | `#DCFCE7` | `#166534` | `check_circle_outline` |
| Em andamento | `#FEF3C7` | `#92400E` | `play_circle_outline` |
| Concluido | `#CCFBF1` | `#115E59` | `task_alt` |
| Cancelado | `#FEE2E2` | `#991B1B` | `cancel_outlined` |
| Falta | `#E2E8F0` | `#334155` | `person_off_outlined` |

Os status ainda nao suportados pela API devem permanecer preparados no tema, mas nao devem aparecer como acoes habilitadas ate a integracao existir.

## 3. Tipografia

Usar **Inter** como fonte principal. Incluir os arquivos da fonte nos assets do Flutter para evitar dependencia de rede durante a apresentacao.

| Estilo | Tamanho | Peso | Uso |
|---|---:|---:|---|
| `displaySmall` | 32 | 700 | Saudacao ou destaque da tela inicial |
| `headlineMedium` | 24 | 700 | Titulo da tela |
| `titleLarge` | 20 | 600 | Titulo de secao ou modal |
| `titleMedium` | 16 | 600 | Titulo de card |
| `bodyLarge` | 16 | 400 | Texto principal e campos |
| `bodyMedium` | 14 | 400 | Conteudo auxiliar |
| `labelLarge` | 16 | 600 | Botoes |
| `labelMedium` | 12 | 600 | Chips e status |

Usar altura de linha entre 1,3 e 1,5. Evitar texto menor que 12 px.

## 4. Espacamento, formas e elevacao

- Escala de espacamento: `4`, `8`, `12`, `16`, `24`, `32` e `48`.
- Margem horizontal padrao em celular: `16`.
- Espaco entre secoes: `24`.
- Altura minima de botao e campo: `48`.
- Alvo minimo de toque: `48 x 48`.
- Raio de campo e botao: `12`.
- Raio de card e modal: `16`.
- Chips de status: raio total (`999`).
- Cards usam borda `#E2E8F0` e sombra muito leve; evitar excesso de elevacao.

## 5. Componentes compartilhados

Todos os integrantes devem reutilizar os mesmos componentes:

- `AppPrimaryButton`: fundo `primary`, texto branco e indicador de carregamento interno.
- `AppSecondaryButton`: fundo transparente, borda `primary` e texto `primary`.
- `AppTextButton`: acao de baixa enfase.
- `AppTextField`: label persistente, ajuda e erro abaixo do campo.
- `AppSearchField`: busca com icone e acao de limpar.
- `StatusChip`: mapeia status, cor, rotulo e icone em um unico lugar.
- `AppointmentCard`: data em destaque, profissional, especialidade, horario e status.
- `ProfessionalCard`: nome, especialidade, CREFITO e estado ativo.
- `EmptyState`: icone, titulo, explicacao curta e acao opcional.
- `ErrorState`: mensagem amigavel e botao "Tentar novamente".
- `AppSkeleton`: carregamento de listas sem mudar bruscamente o layout.
- `AppSnackbar`: sucesso, aviso e erro com icone e texto.
- `ConfirmDialog`: confirmacao de cancelamento ou outra acao destrutiva.

## 6. Navegacao por perfil

### Paciente

Barra inferior com quatro destinos:

1. **Inicio** - resumo e proxima consulta.
2. **Agendar** - fluxo principal de novo agendamento.
3. **Agenda** - proximos agendamentos e historico.
4. **Perfil** - dados pessoais e sessao.

### Fisioterapeuta

Barra inferior com quatro destinos:

1. **Inicio** - resumo do dia.
2. **Agenda** - consultas e disponibilidade.
3. **Pacientes** - pacientes vinculados e historico autorizado.
4. **Perfil** - dados profissionais e especialidade.

### Clinica

O perfil Clinica e administrativo e tem baixa prioridade para a apresentacao. Quando implementado, deve usar navegacao propria, sem exibir funcoes administrativas aos demais perfis.

## 7. Padrao das telas

- Titulo alinhado a esquerda e uma acao principal por tela.
- Listas usam cards separados por `12` px.
- Filtros secundarios aparecem em bottom sheet, evitando barras apertadas.
- Formularios longos sao divididos em secoes com titulos claros.
- A acao principal do formulario fica visivel ao final e mostra carregamento ao enviar.
- Datas devem aparecer como `dd/MM/yyyy`; horarios como `HH:mm`.
- Exibir datas no fuso local, mesmo quando a API usar UTC.
- Mensagens devem explicar a proxima acao: por exemplo, "Esse horario acabou de ser reservado. Escolha outro horario."

## 8. Estados obrigatorios

Toda tela que consome API precisa prever:

1. Carregamento inicial.
2. Conteudo carregado.
3. Lista vazia.
4. Erro recuperavel com nova tentativa.
5. Sem conexao.
6. Sessao expirada.
7. Acao em andamento, impedindo envio duplicado.
8. Sucesso com confirmacao visual.

## 9. Acessibilidade

- Manter contraste minimo WCAG AA.
- Nao comunicar status somente por cor.
- Adicionar `Semantics` aos icones que executam acoes.
- Respeitar aumento de fonte sem cortar botoes ou informacoes.
- Usar labels visiveis nos formularios; placeholder nao substitui label.
- Teclado, mascara e autofill devem corresponder ao tipo do campo.
- Ordem de foco deve acompanhar a ordem visual.

## 10. Responsividade

- Projetar primeiro para larguras de `360` a `430` px.
- Em tablets a partir de `768` px, limitar formularios a `640` px e centraliza-los.
- Listas podem usar duas colunas em tablet quando os cards continuarem legiveis.
- Nao fixar altura de conteudo com texto; permitir expansao.

## 11. Tokens sugeridos para Flutter

```dart
abstract final class AppColors {
  static const primary = Color(0xFF0F766E);
  static const primaryDark = Color(0xFF115E59);
  static const primaryLight = Color(0xFFCCFBF1);
  static const secondary = Color(0xFF2563EB);
  static const background = Color(0xFFF8FAFC);
  static const surface = Color(0xFFFFFFFF);
  static const surfaceVariant = Color(0xFFF1F5F9);
  static const textPrimary = Color(0xFF0F172A);
  static const textSecondary = Color(0xFF475569);
  static const border = Color(0xFFCBD5E1);
  static const divider = Color(0xFFE2E8F0);
  static const error = Color(0xFFB91C1C);
  static const success = Color(0xFF15803D);
  static const warning = Color(0xFFB45309);
}
```

O tema deve ser implementado uma unica vez em `ThemeData`/`ColorScheme`. As telas nao devem conter cores hexadecimais ou estilos de texto avulsos.

## 12. Identidade e logo

Para a primeira versao, usar um simbolo simples formado por **calendario + movimento humano**, acompanhado do nome **AgendaiFisio**. O simbolo deve funcionar em uma cor, em tamanho pequeno e como icone do aplicativo.

Nao usar cruz medica, imagens anatomicas detalhadas ou degradês em todos os componentes. Um degrade discreto de `#0F766E` para `#2563EB` pode ser reservado para a tela de boas-vindas e materiais de apresentacao.

## 13. Definition of Done visual

Uma task de frontend so esta visualmente concluida quando:

- Usa os tokens e componentes compartilhados.
- Possui carregamento, vazio, erro e sucesso.
- Funciona em 360 px sem overflow.
- Funciona com texto ampliado a 150%.
- Nao contem IDs tecnicos expostos ao usuario.
- Nao usa cor como unico indicador de estado.
- Mantem consistencia com as telas dos dois grupos.
- Foi comparada com a referencia visual aprovada antes do merge.
