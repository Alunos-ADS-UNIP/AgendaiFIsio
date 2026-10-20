import 'package:flutter/material.dart';

import '../../domain/repositories/especialidade_repository.dart';
import '../controllers/especialidade_controller.dart';

class CriarEspecialidadePage extends StatefulWidget {
  final EspecialidadeCatalogController catalogController;
  final EspecialidadeRepository repository;

  const CriarEspecialidadePage({
    super.key,
    required this.catalogController,
    required this.repository,
  });

  @override
  State<CriarEspecialidadePage> createState() => _CriarEspecialidadePageState();
}

class _CriarEspecialidadePageState extends State<CriarEspecialidadePage> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  late final ClinicaEspecialidadeController _controller;

  @override
  void initState() {
    super.initState();
    _controller = ClinicaEspecialidadeController(repository: widget.repository);
  }

  @override
  void dispose() {
    _controller.dispose();
    _nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Nova especialidade')),
    body: SafeArea(
      child: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              'Cadastrar especialidade',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            const Text(
              'A nova opção ficará disponível no catálogo para os profissionais.',
            ),
            const SizedBox(height: 24),
            TextFormField(
              controller: _nameController,
              maxLength: 100,
              textCapitalization: TextCapitalization.sentences,
              decoration: const InputDecoration(
                labelText: 'Nome da especialidade',
                hintText: 'Ex.: Fisioterapia Esportiva',
              ),
              validator: _validateName,
            ),
            const SizedBox(height: 8),
            AnimatedBuilder(
              animation: _controller,
              builder: (context, _) => Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (_controller.errorMessage != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(
                        _controller.errorMessage!,
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.error,
                        ),
                      ),
                    ),
                  ElevatedButton.icon(
                    onPressed: _controller.isSaving ? null : _submit,
                    icon: _controller.isSaving
                        ? const SizedBox.square(
                            dimension: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.add_rounded),
                    label: const Text('Cadastrar especialidade'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    ),
  );

  String? _validateName(String? value) {
    final normalized = _normalize(value ?? '');
    if (normalized.isEmpty) {
      return 'Informe o nome da especialidade.';
    }
    if (normalized.length < 3 || normalized.length > 100) {
      return 'Use entre 3 e 100 caracteres.';
    }
    return null;
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final created = await _controller.create(_normalize(_nameController.text));
    if (!mounted || created == null) return;
    widget.catalogController.addCreated(created);
    _nameController.clear();
    _formKey.currentState!.reset();
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Especialidade cadastrada com sucesso.')),
    );
  }

  String _normalize(String value) =>
      value.trim().replaceAll(RegExp(r'\s+'), ' ');
}
