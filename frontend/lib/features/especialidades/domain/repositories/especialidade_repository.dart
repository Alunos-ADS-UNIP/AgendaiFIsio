import '../entities/especialidade.dart';

abstract interface class EspecialidadeRepository {
  Future<List<Especialidade>> listar();
  Future<Especialidade> obterPorId(String id);
  Future<Especialidade> criar(String nome);
  Future<void> atualizarEspecialidadeProfissional(String especialidadeId);
}
