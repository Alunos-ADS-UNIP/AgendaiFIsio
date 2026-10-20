import 'package:flutter/material.dart';

import '../core/session/session.dart';
import '../core/theme/app_theme.dart';
import '../features/especialidades/data/repositories/especialidade_repository_impl.dart';
import '../features/especialidades/presentation/controllers/especialidade_controller.dart';
import '../features/especialidades/presentation/pages/criar_especialidade_page.dart';
import '../features/especialidades/presentation/pages/especialidade_selector_page.dart';
import '../features/especialidades/presentation/pages/especialidades_page.dart';

class AgendaiFisioApp extends StatefulWidget {
  final SessionStore sessionStore;
  final EspecialidadeRepositoryImpl especialidadeRepository;

  const AgendaiFisioApp({
    super.key,
    required this.sessionStore,
    required this.especialidadeRepository,
  });

  @override
  State<AgendaiFisioApp> createState() => _AgendaiFisioAppState();
}

class _AgendaiFisioAppState extends State<AgendaiFisioApp> {
  late final EspecialidadeCatalogController catalogController;

  @override
  void initState() {
    super.initState();
    catalogController = EspecialidadeCatalogController(
      repository: widget.especialidadeRepository,
    )..load();
  }

  @override
  void dispose() {
    catalogController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'AgendaiFisio',
    debugShowCheckedModeBanner: false,
    theme: buildAppTheme(),
    routes: {
      '/': (_) => EspecialidadesPage(controller: catalogController),
      '/especialidades': (_) =>
          EspecialidadesPage(controller: catalogController),
      '/profissional/especialidade': (_) => RoleGate(
        requiredRole: UserRole.profissional,
        sessionStore: widget.sessionStore,
        child: EspecialidadeSelectorPage(
          catalogController: catalogController,
          repository: widget.especialidadeRepository,
        ),
      ),
      '/clinica/especialidades/nova': (_) => RoleGate(
        requiredRole: UserRole.clinica,
        sessionStore: widget.sessionStore,
        child: CriarEspecialidadePage(
          catalogController: catalogController,
          repository: widget.especialidadeRepository,
        ),
      ),
    },
  );
}

class RoleGate extends StatelessWidget {
  final UserRole requiredRole;
  final SessionStore sessionStore;
  final Widget child;

  const RoleGate({
    super.key,
    required this.requiredRole,
    required this.sessionStore,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    final session = sessionStore.current;
    if (session == null || !session.isValid) {
      return const _AccessDeniedPage(
        message: 'Entre na sua conta para acessar esta área.',
      );
    }
    if (session.role != requiredRole) {
      return const _AccessDeniedPage(
        message: 'Esta funcionalidade não está disponível para o seu perfil.',
      );
    }
    return child;
  }
}

class _AccessDeniedPage extends StatelessWidget {
  final String message;

  const _AccessDeniedPage({required this.message});

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Acesso restrito')),
    body: Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Text(message, textAlign: TextAlign.center),
      ),
    ),
  );
}
