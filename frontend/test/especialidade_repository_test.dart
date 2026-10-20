import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:agendai_fisio_frontend/core/errors/api_exception.dart';
import 'package:agendai_fisio_frontend/core/network/api_client.dart';
import 'package:agendai_fisio_frontend/core/session/session.dart';
import 'package:agendai_fisio_frontend/features/especialidades/data/repositories/especialidade_repository_impl.dart';

void main() {
  test(
    'lista especialidades publicamente sem Bearer e elimina duplicidade visual',
    () async {
      final session = MemorySessionStore();
      final client = MockClient((request) async {
        expect(request.url.path, '/api/especialidade');
        expect(request.headers.containsKey('authorization'), isFalse);
        return http.Response(
          jsonEncode([
            {'id': '1', 'nome': 'Acupuntura'},
            {'id': '1', 'nome': 'Acupuntura'},
          ]),
          200,
        );
      });
      final repository = EspecialidadeRepositoryImpl(
        apiClient: ApiClient(
          baseUrl: 'https://example.test/api',
          sessionStore: session,
          httpClient: client,
        ),
      );

      final result = await repository.listar();

      expect(result, hasLength(1));
      expect(result.single.nome, 'Acupuntura');
    },
  );

  test('consulta especialidade por GUID', () async {
    final client = MockClient((request) async {
      expect(request.url.path, '/api/especialidade/abc');
      return http.Response(
        jsonEncode({'id': 'abc', 'nome': 'Osteopatia'}),
        200,
      );
    });
    final repository = EspecialidadeRepositoryImpl(
      apiClient: ApiClient(
        baseUrl: 'https://example.test/api',
        sessionStore: MemorySessionStore(),
        httpClient: client,
      ),
    );

    final result = await repository.obterPorId('abc');

    expect(result.id, 'abc');
  });

  test('envia vínculo autenticado no contrato real', () async {
    final session = MemorySessionStore();
    await session.save(
      AuthSession(
        accessToken: 'token',
        usuarioId: 'user',
        perfilId: 'profile',
        email: 'profissional@example.com',
        role: UserRole.profissional,
        expiresAtUtc: DateTime.now().toUtc().add(const Duration(hours: 1)),
      ),
    );
    final client = MockClient((request) async {
      expect(request.method, 'PUT');
      expect(request.url.path, '/api/profissional/especialidade');
      expect(request.headers['authorization'], 'Bearer token');
      expect(jsonDecode(request.body), {'especialidadeId': 'abc'});
      return http.Response(
        jsonEncode({'mensagem': 'Especialidade atualizada com sucesso!'}),
        200,
      );
    });
    final repository = EspecialidadeRepositoryImpl(
      apiClient: ApiClient(
        baseUrl: 'https://example.test/api',
        sessionStore: session,
        httpClient: client,
      ),
    );

    await repository.atualizarEspecialidadeProfissional('abc');
  });

  test('mantém conflito HTTP 409 como erro de domínio', () async {
    final session = MemorySessionStore();
    await session.save(
      AuthSession(
        accessToken: 'token',
        usuarioId: 'clinic',
        perfilId: null,
        email: 'clinic@example.com',
        role: UserRole.clinica,
        expiresAtUtc: DateTime.now().toUtc().add(const Duration(hours: 1)),
      ),
    );
    final client = MockClient(
      (_) async => http.Response(
        jsonEncode({
          'erro': 'Já existe uma especialidade cadastrada com este nome.',
        }),
        409,
      ),
    );
    final repository = EspecialidadeRepositoryImpl(
      apiClient: ApiClient(
        baseUrl: 'https://example.test/api',
        sessionStore: session,
        httpClient: client,
      ),
    );

    await expectLater(
      repository.criar('Aquática'),
      throwsA(
        isA<ApiException>().having((e) => e.statusCode, 'statusCode', 409),
      ),
    );
  });

  test('mapeia respostas HTTP de validação e autorização', () async {
    for (final status in [400, 401, 403, 404]) {
      final client = MockClient((_) async => http.Response('{}', status));
      final repository = EspecialidadeRepositoryImpl(
        apiClient: ApiClient(
          baseUrl: 'https://example.test/api',
          sessionStore: MemorySessionStore(),
          httpClient: client,
        ),
      );

      await expectLater(
        repository.obterPorId('abc'),
        throwsA(
          isA<ApiException>().having((e) => e.statusCode, 'statusCode', status),
        ),
      );
    }
  });
}
