import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../errors/api_exception.dart';
import '../session/session.dart';

class ApiClient {
  final String baseUrl;
  final http.Client _httpClient;
  final SessionStore sessionStore;

  ApiClient({
    required this.baseUrl,
    required this.sessionStore,
    http.Client? httpClient,
  }) : _httpClient = httpClient ?? http.Client();

  Future<dynamic> getJson(String path, {bool authenticated = false}) =>
      _send('GET', path, authenticated: authenticated);

  Future<dynamic> postJson(
    String path,
    Map<String, dynamic> body, {
    bool authenticated = true,
  }) => _send('POST', path, body: body, authenticated: authenticated);

  Future<dynamic> putJson(
    String path,
    Map<String, dynamic> body, {
    bool authenticated = true,
  }) => _send('PUT', path, body: body, authenticated: authenticated);

  Future<dynamic> _send(
    String method,
    String path, {
    Map<String, dynamic>? body,
    required bool authenticated,
  }) async {
    final uri = Uri.parse('$baseUrl$path');
    final headers = <String, String>{'Accept': 'application/json'};
    if (body != null) headers['Content-Type'] = 'application/json';
    if (authenticated) {
      final token = sessionStore.current?.accessToken;
      if (token == null || token.isEmpty) {
        throw const ApiException(401, 'Sua sessão expirou. Entre novamente.');
      }
      headers['Authorization'] = 'Bearer $token';
    }

    try {
      final response = switch (method) {
        'GET' => await _httpClient.get(uri, headers: headers),
        'POST' => await _httpClient.post(
          uri,
          headers: headers,
          body: jsonEncode(body),
        ),
        'PUT' => await _httpClient.put(
          uri,
          headers: headers,
          body: jsonEncode(body),
        ),
        _ => throw StateError('Método HTTP não suportado'),
      };
      if (response.statusCode == 401 && authenticated) {
        await sessionStore.clear();
      }
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw ApiException(response.statusCode, _messageFrom(response));
      }
      if (response.body.trim().isEmpty) return null;
      return jsonDecode(response.body);
    } on ApiException {
      rethrow;
    } on TimeoutException {
      throw const ApiException(0, 'A conexão demorou demais. Tente novamente.');
    } on FormatException {
      throw const ApiException(0, 'A resposta da API é inválida.');
    } catch (_) {
      throw const ApiException(0, 'Não foi possível conectar ao servidor.');
    }
  }

  String _messageFrom(http.Response response) {
    try {
      final data = jsonDecode(response.body);
      if (data is Map<String, dynamic>) {
        final value = data['erro'] ?? data['message'] ?? data['title'];
        if (value is String && value.isNotEmpty) return value;
      }
    } catch (_) {}
    return switch (response.statusCode) {
      400 => 'Verifique os dados informados.',
      401 => 'Sua sessão expirou. Entre novamente.',
      403 => 'Você não tem permissão para realizar esta ação.',
      404 => 'O recurso solicitado não foi encontrado.',
      409 => 'A operação entrou em conflito com um dado existente.',
      _ => 'O servidor não conseguiu concluir a operação.',
    };
  }
}
