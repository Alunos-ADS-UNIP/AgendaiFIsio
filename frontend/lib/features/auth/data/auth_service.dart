import '../../../core/network/api_client.dart';
import '../../../core/session/session.dart';
import 'auth_dto.dart';

class AuthService {
  final ApiClient apiClient;
  final SessionStore sessionStore;

  const AuthService({required this.apiClient, required this.sessionStore});

  Future<AuthSession> login({
    required String email,
    required String senha,
  }) async {
    final json = await apiClient.postJson('/auth/login', {
      'email': email,
      'senha': senha,
    }, authenticated: false) as Map<String, dynamic>;
    final session = AuthResponseDto.fromJson(json).toSession();
    await sessionStore.save(session);
    return session;
  }

  Future<void> validateSession() async {
    final json = await apiClient.getJson(
      '/auth/me',
      authenticated: true,
    ) as Map<String, dynamic>;
    final role = userRoleFromString(json['tipoUsuario'] as String?);
    if (role == null) throw StateError('Perfil de usuário inválido.');
  }

  Future<void> logout() async {
    try {
      if (sessionStore.current != null) {
        await apiClient.postJson('/auth/logout', const {}, authenticated: true);
      }
    } finally {
      await sessionStore.clear();
    }
  }
}
