class Especialidade {
  final String id;
  final String nome;

  const Especialidade({required this.id, required this.nome});

  @override
  bool operator ==(Object other) => other is Especialidade && other.id == id;

  @override
  int get hashCode => id.hashCode;
}
