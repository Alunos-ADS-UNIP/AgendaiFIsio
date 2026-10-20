# AgendaiFisio — Frontend Flutter

Base Flutter preparada para o Grupo B, com a B-FE-03 implementada sobre os contratos de
`origin/main`.

## Arquitetura

- Organização por feature em `lib/features/especialidades`.
- `ApiClient` centraliza base URL, JSON, Bearer e tratamento de `401`.
- `SecureSessionStore` guarda a sessão em armazenamento seguro.
- `ChangeNotifier` foi escolhido para manter o estado simples e sem uma dependência adicional.
- A UI não faz chamadas HTTP diretamente; usa repository e controllers.

## Executar

```bash
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:5048/api
```

O valor padrão é `http://localhost:5048/api`. Para HTTPS, informe a URL completa com
`--dart-define=API_BASE_URL=...`.

As rotas públicas de especialidades funcionam sem sessão. As telas de vínculo profissional e
cadastro pela Clínica exigem uma sessão válida com o respectivo perfil; a tela de login ainda
deve ser conectada ao fluxo compartilhado do Grupo B.

## Design system

O tema Material 3 está em `lib/core/theme/app_theme.dart`, com os tokens do documento oficial.
O projeto usa `fontFamily: Inter`; os arquivos de fonte ainda precisam ser adicionados pelo
Grupo B em `assets/fonts/` quando o pacote oficial de assets for disponibilizado.

## Verificações

```bash
dart format .
flutter analyze
flutter test
```

A new Flutter project.
