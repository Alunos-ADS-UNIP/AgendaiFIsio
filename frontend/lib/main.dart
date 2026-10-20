import 'package:flutter/material.dart';

import 'app/app.dart';
import 'core/network/api_client.dart';
import 'core/session/session.dart';
import 'features/especialidades/data/repositories/especialidade_repository_impl.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5048/api',
  );
  final sessionStore = SecureSessionStore();
  await sessionStore.load();
  final apiClient = ApiClient(baseUrl: apiBaseUrl, sessionStore: sessionStore);
  final repository = EspecialidadeRepositoryImpl(apiClient: apiClient);

  runApp(
    AgendaiFisioApp(
      sessionStore: sessionStore,
      especialidadeRepository: repository,
    ),
  );
}
