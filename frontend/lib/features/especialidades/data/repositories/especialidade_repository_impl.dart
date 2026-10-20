import '../../../../core/network/api_client.dart';
import '../../domain/entities/especialidade.dart';
import '../../domain/repositories/especialidade_repository.dart';
import '../dtos/especialidade_dto.dart';

class EspecialidadeRepositoryImpl implements EspecialidadeRepository {
  final ApiClient apiClient;

  const EspecialidadeRepositoryImpl({required this.apiClient});

  @override
  Future<List<Especialidade>> listar() async {
    final json = await apiClient.getJson(
      '/especialidade',
      authenticated: false,
    ) as List<dynamic>;
    final unique = <String, Especialidade>{};
    for (final item in json) {
      final dto = EspecialidadeDto.fromJson(item as Map<String, dynamic>);
      unique[dto.id] = dto.toEntity();
    }
    return unique.values.toList(growable: false);
  }

  @override
  Future<Especialidade> obterPorId(String id) async {
    final json = await apiClient.getJson(
      '/especialidade/$id',
      authenticated: false,
    ) as Map<String, dynamic>;
    return EspecialidadeDto.fromJson(json).toEntity();
  }

  @override
  Future<Especialidade> criar(String nome) async {
    final json = await apiClient.postJson(
      '/especialidade',
      EspecialidadeCreateDto(nome).toJson(),
      authenticated: true,
    ) as Map<String, dynamic>;
    return EspecialidadeDto.fromJson(json).toEntity();
  }

  @override
  Future<void> atualizarEspecialidadeProfissional(
    String especialidadeId,
  ) async {
    await apiClient.putJson(
      '/profissional/especialidade',
      ProfissionalEspecialidadeUpdateDto(especialidadeId).toJson(),
      authenticated: true,
    );
  }
}
