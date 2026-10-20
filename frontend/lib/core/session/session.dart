import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

enum UserRole { paciente, profissional, clinica }

UserRole? userRoleFromString(String? value) {
  switch (value?.trim().toLowerCase()) {
    case 'paciente':
      return UserRole.paciente;
    case 'profissional':
      return UserRole.profissional;
    case 'clinica':
      return UserRole.clinica;
    default:
      return null;
  }
}

class AuthSession {
  final String accessToken;
  final String usuarioId;
  final String? perfilId;
  final String email;
  final UserRole role;
  final DateTime expiresAtUtc;

  const AuthSession({
    required this.accessToken,
    required this.usuarioId,
    required this.perfilId,
    required this.email,
    required this.role,
    required this.expiresAtUtc,
  });

  bool get isValid =>
      accessToken.isNotEmpty && expiresAtUtc.isAfter(DateTime.now().toUtc());
}

abstract interface class SessionStore {
  AuthSession? get current;
  Future<void> load();
  Future<void> save(AuthSession session);
  Future<void> clear();
}

class SecureSessionStore implements SessionStore {
  static const _sessionKey = 'agendai_fisio.session';
  final FlutterSecureStorage _storage;
  AuthSession? _current;

  SecureSessionStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  @override
  AuthSession? get current => _current;

  @override
  Future<void> load() async {
    final raw = await _storage.read(key: _sessionKey);
    if (raw == null) return;
    try {
      final json = jsonDecode(raw) as Map<String, dynamic>;
      final role = userRoleFromString(json['role'] as String?);
      if (role == null) {
        await clear();
        return;
      }
      final session = AuthSession(
        accessToken: json['accessToken'] as String? ?? '',
        usuarioId: json['usuarioId'] as String? ?? '',
        perfilId: json['perfilId'] as String?,
        email: json['email'] as String? ?? '',
        role: role,
        expiresAtUtc: DateTime.parse(json['expiresAtUtc'] as String),
      );
      if (session.isValid) _current = session;
    } catch (_) {
      await clear();
    }
  }

  @override
  Future<void> save(AuthSession session) async {
    _current = session;
    await _storage.write(
      key: _sessionKey,
      value: jsonEncode({
        'accessToken': session.accessToken,
        'usuarioId': session.usuarioId,
        'perfilId': session.perfilId,
        'email': session.email,
        'role': session.role.name,
        'expiresAtUtc': session.expiresAtUtc.toUtc().toIso8601String(),
      }),
    );
  }

  @override
  Future<void> clear() async {
    _current = null;
    await _storage.delete(key: _sessionKey);
  }
}

class MemorySessionStore implements SessionStore {
  AuthSession? _current;

  @override
  AuthSession? get current => _current;

  @override
  Future<void> load() async {}

  @override
  Future<void> save(AuthSession session) async => _current = session;

  @override
  Future<void> clear() async => _current = null;
}
