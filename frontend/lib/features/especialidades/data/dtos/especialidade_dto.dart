import '../../domain/entities/especialidade.dart';

class EspecialidadeDto {
  final String id;
  final String nome;

  const EspecialidadeDto({required this.id, required this.nome});

  factory EspecialidadeDto.fromJson(Map<String, dynamic> json) =>
      EspecialidadeDto(id: json['id'] as String, nome: json['nome'] as String);

  Especialidade toEntity() => Especialidade(id: id, nome: nome);
}

class EspecialidadeCreateDto {
  final String nome;

  const EspecialidadeCreateDto(this.nome);

  Map<String, dynamic> toJson() => {'nome': nome};
}

class ProfissionalEspecialidadeUpdateDto {
  final String especialidadeId;

  const ProfissionalEspecialidadeUpdateDto(this.especialidadeId);

  Map<String, dynamic> toJson() => {'especialidadeId': especialidadeId};
}
