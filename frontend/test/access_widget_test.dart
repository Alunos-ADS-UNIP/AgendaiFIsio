import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:agendai_fisio_frontend/app/app.dart';
import 'package:agendai_fisio_frontend/core/session/session.dart';

void main() {
  testWidgets('bloqueia área de Clínica para sessão de Profissional', (
    tester,
  ) async {
    final session = MemorySessionStore();
    await session.save(
      AuthSession(
        accessToken: 'token',
        usuarioId: 'user',
        perfilId: 'profile',
        email: 'user@example.com',
        role: UserRole.profissional,
        expiresAtUtc: DateTime.now().toUtc().add(const Duration(hours: 1)),
      ),
    );

    await tester.pumpWidget(
      MaterialApp(
        home: RoleGate(
          requiredRole: UserRole.clinica,
          sessionStore: session,
          child: const Text('privado'),
        ),
      ),
    );

    expect(
      find.text('Esta funcionalidade não está disponível para o seu perfil.'),
      findsOneWidget,
    );
    expect(find.text('privado'), findsNothing);
  });

  testWidgets('catálogo mantém layout em 360 px e texto ampliado', (
    tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(360, 760));
    await tester.pumpWidget(
      MediaQuery(
        data: const MediaQueryData(textScaler: TextScaler.linear(1.5)),
        child: MaterialApp(
          home: Scaffold(
            body: ListView(
              padding: const EdgeInsets.all(16),
              children: const [
                Text('Especialidades'),
                TextField(
                  decoration: InputDecoration(
                    labelText: 'Pesquisar especialidade',
                  ),
                ),
                SizedBox(height: 48),
                Text('Acupuntura'),
              ],
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    expect(tester.takeException(), isNull);
    await tester.binding.setSurfaceSize(null);
  });
}
