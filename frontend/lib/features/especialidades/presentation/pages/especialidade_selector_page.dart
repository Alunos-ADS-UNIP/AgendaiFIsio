import 'package:flutter/material.dart';

import '../../domain/entities/especialidade.dart';
import '../../domain/repositories/especialidade_repository.dart';
import '../controllers/especialidade_controller.dart';

class EspecialidadeSelectorPage extends StatefulWidget {
  final EspecialidadeCatalogController catalogController;
  final EspecialidadeRepository repository;

  const EspecialidadeSelectorPage({
    super.key,
    required this.catalogController,
    required this.repository,
  });

  @override
  State<EspecialidadeSelectorPage> createState() =>
      _EspecialidadeSelectorPageState();
}

class _EspecialidadeSelectorPageState extends State<EspecialidadeSelectorPage> {
  late final ProfissionalEspecialidadeController _controller;
  final _searchController = TextEditingController();
  Especialidade? _selected;

  @override
  void initState() {
    super.initState();
    _controller = ProfissionalEspecialidadeController(
      repository: widget.repository,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Minha especialidade')),
    body: AnimatedBuilder(
      animation: Listenable.merge([widget.catalogController, _controller]),
      builder: (context, _) {
        final items = widget.catalogController.state.items;
        final query = _searchController.text.trim().toLowerCase();
        final filtered = items
            .where((item) => item.nome.toLowerCase().contains(query))
            .toList(growable: false);
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              'Escolha uma especialidade',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            const Text(
              'A escolha será vinculada ao seu perfil somente após a confirmação da API.',
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _searchController,
              onChanged: (_) => setState(() {}),
              decoration: const InputDecoration(
                labelText: 'Pesquisar no catálogo',
                prefixIcon: Icon(Icons.search_rounded),
              ),
            ),
            const SizedBox(height: 12),
            ...filtered.map(
              (item) => Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  onTap: () => setState(() => _selected = item),
                  leading: Icon(
                    _selected?.id == item.id
                        ? Icons.radio_button_checked
                        : Icons.radio_button_unchecked,
                    color: _selected?.id == item.id
                        ? Theme.of(context).colorScheme.primary
                        : null,
                  ),
                  title: Text(item.nome),
                  subtitle: const Text('Selecionar esta opção'),
                  selected: _selected?.id == item.id,
                ),
              ),
            ),
            if (filtered.isEmpty)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Text('Nenhuma opção encontrada.'),
              ),
            const SizedBox(height: 16),
            if (_controller.errorMessage != null)
              Text(
                _controller.errorMessage!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ElevatedButton.icon(
              onPressed: _selected == null || _controller.isSaving
                  ? null
                  : _save,
              icon: _controller.isSaving
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.save_outlined),
              label: const Text('Salvar especialidade'),
            ),
          ],
        );
      },
    ),
  );

  Future<void> _save() async {
    final selected = _selected;
    if (selected == null) return;
    final success = await _controller.save(selected.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          success
              ? 'Especialidade atualizada com sucesso.'
              : 'Não foi possível atualizar a especialidade.',
        ),
        backgroundColor: success
            ? Colors.green.shade700
            : Theme.of(context).colorScheme.error,
      ),
    );
  }
}
