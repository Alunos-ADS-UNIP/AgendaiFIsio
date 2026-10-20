import 'package:flutter_test/flutter_test.dart';

import 'package:agendai_fisio_frontend/features/especialidades/domain/entities/especialidade.dart';
import 'package:agendai_fisio_frontend/features/especialidades/domain/repositories/especialidade_repository.dart';
import 'package:agendai_fisio_frontend/features/especialidades/presentation/controllers/especialidade_controller.dart';

class FakeRepository implements EspecialidadeRepository {
  List<Especialidade> items = const [
    Especialidade(id: '1', nome: 'Acupuntura'),
  ];
  bool shouldFail = false;
  String? savedId;
  String? createdName;

  @override
  Future<List<Especialidade>> listar() async {
    if (shouldFail) throw const FormatException('offline');
    return items;
  }

  @override
  Future<Especialidade> obterPorId(String id) async => items.first;

  @override
  Future<Especialidade> criar(String nome) async {
    createdName = nome;
    return Especialidade(id: '2', nome: nome);
  }

  @override
  Future<void> atualizarEspecialidadeProfissional(
    String especialidadeId,
  ) async => savedId = especialidadeId;
}

void main() {
  test('carrega catálogo e remove duplicidade por id', () async {
    final fake = FakeRepository()
      ..items = const [
        Especialidade(id: '1', nome: 'A'),
        Especialidade(id: '1', nome: 'A'),
      ];
    final controller = EspecialidadeCatalogController(repository: fake);

    await controller.load();

    expect(controller.state.status, CatalogStatus.loaded);
    expect(controller.state.items, hasLength(1));
    controller.dispose();
  });

  test('só atualiza seleção após sucesso do repository', () async {
    final fake = FakeRepository();
    final controller = ProfissionalEspecialidadeController(repository: fake);

    expect(await controller.save('1'), isTrue);
    expect(controller.selectedId, '1');
    expect(fake.savedId, '1');
    controller.dispose();
  });

  test('cadastro retorna item para atualização do catálogo local', () async {
    final fake = FakeRepository();
    final controller = ClinicaEspecialidadeController(repository: fake);

    final created = await controller.create('Nova Área');

    expect(created?.nome, 'Nova Área');
    expect(fake.createdName, 'Nova Área');
    controller.dispose();
  });
}
