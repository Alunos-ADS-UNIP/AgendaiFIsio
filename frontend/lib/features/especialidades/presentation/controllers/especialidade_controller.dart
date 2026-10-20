import 'package:flutter/foundation.dart';

import '../../../../core/errors/api_exception.dart';
import '../../domain/entities/especialidade.dart';
import '../../domain/repositories/especialidade_repository.dart';

enum CatalogStatus { initial, loading, loaded, empty, error, offline }

class EspecialidadeCatalogState {
  final CatalogStatus status;
  final List<Especialidade> items;
  final String? errorMessage;

  const EspecialidadeCatalogState({
    this.status = CatalogStatus.initial,
    this.items = const [],
    this.errorMessage,
  });

  EspecialidadeCatalogState copyWith({
    CatalogStatus? status,
    List<Especialidade>? items,
    String? errorMessage,
    bool clearError = false,
  }) => EspecialidadeCatalogState(
    status: status ?? this.status,
    items: items ?? this.items,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}

class EspecialidadeCatalogController extends ChangeNotifier {
  final EspecialidadeRepository repository;
  EspecialidadeCatalogState _state = const EspecialidadeCatalogState();

  EspecialidadeCatalogController({required this.repository});

  EspecialidadeCatalogState get state => _state;

  Future<void> load({bool refresh = false}) async {
    if (!refresh) {
      _state = _state.copyWith(status: CatalogStatus.loading, clearError: true);
      notifyListeners();
    }
    try {
      final items = _deduplicate(await repository.listar());
      _state = _state.copyWith(
        status: items.isEmpty ? CatalogStatus.empty : CatalogStatus.loaded,
        items: items,
        clearError: true,
      );
    } on ApiException catch (error) {
      _state = _state.copyWith(
        status: error.isOffline ? CatalogStatus.offline : CatalogStatus.error,
        errorMessage: error.message,
      );
    }
    notifyListeners();
  }

  void addCreated(Especialidade item) {
    final exists = _state.items.any(
      (current) =>
          current.id == item.id ||
          _normalize(current.nome) == _normalize(item.nome),
    );
    if (exists) return;
    final items = [..._state.items, item]
      ..sort((a, b) => a.nome.toLowerCase().compareTo(b.nome.toLowerCase()));
    _state = _state.copyWith(status: CatalogStatus.loaded, items: items);
    notifyListeners();
  }

  List<Especialidade> _deduplicate(List<Especialidade> source) {
    final unique = <String, Especialidade>{};
    for (final item in source) {
      unique[item.id] = item;
    }
    return unique.values.toList(growable: false);
  }

  static String _normalize(String value) =>
      value.trim().replaceAll(RegExp(r'\s+'), ' ').toLowerCase();
}

class ProfissionalEspecialidadeController extends ChangeNotifier {
  final EspecialidadeRepository repository;
  bool isSaving = false;
  String? selectedId;
  String? errorMessage;

  ProfissionalEspecialidadeController({
    required this.repository,
    String? initialId,
  }) : selectedId = initialId;

  Future<bool> save(String id) async {
    isSaving = true;
    errorMessage = null;
    notifyListeners();
    try {
      await repository.atualizarEspecialidadeProfissional(id);
      selectedId = id;
      return true;
    } on ApiException catch (error) {
      errorMessage = error.message;
      return false;
    } finally {
      isSaving = false;
      notifyListeners();
    }
  }
}

class ClinicaEspecialidadeController extends ChangeNotifier {
  final EspecialidadeRepository repository;
  bool isSaving = false;
  String? errorMessage;

  ClinicaEspecialidadeController({required this.repository});

  Future<Especialidade?> create(String nome) async {
    isSaving = true;
    errorMessage = null;
    notifyListeners();
    try {
      return await repository.criar(nome);
    } on ApiException catch (error) {
      errorMessage = error.message;
      return null;
    } finally {
      isSaving = false;
      notifyListeners();
    }
  }
}
