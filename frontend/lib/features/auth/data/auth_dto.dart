import '../../../core/session/session.dart';

class AuthResponseDto {
  final String accessToken;
  final String usuarioId;
  final String? perfilId;
  final String email;
  final UserRole role;
  final DateTime expiresAtUtc;

  const AuthResponseDto({
    required this.accessToken,
    required this.usuarioId,
    required this.perfilId,
    required this.email,
    required this.role,
    required this.expiresAtUtc,
  });

  factory AuthResponseDto.fromJson(Map<String, dynamic> json) {
    final usuario = (json['usuario'] as Map<String, dynamic>?) ?? const {};
    final role = userRoleFromString(usuario['tipoUsuario'] as String?);
    if (role == null) {
      throw const FormatException('Perfil de usuário inválido.');
    }
    return AuthResponseDto(
      accessToken: (json['accessToken'] ?? json['token']) as String,
      usuarioId: usuario['usuarioId'] as String,
      perfilId: usuario['perfilId'] as String?,
      email: usuario['email'] as String,
      role: role,
      expiresAtUtc: DateTime.parse(json['expiresAtUtc'] as String).toUtc(),
    );
  }

  AuthSession toSession() => AuthSession(
    accessToken: accessToken,
    usuarioId: usuarioId,
    perfilId: perfilId,
    email: email,
    role: role,
    expiresAtUtc: expiresAtUtc,
  );
}
