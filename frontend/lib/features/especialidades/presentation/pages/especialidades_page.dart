import 'package:flutter/material.dart';

import '../../../../core/widgets/app_states.dart';
import '../../domain/entities/especialidade.dart';
import '../controllers/especialidade_controller.dart';

class EspecialidadesPage extends StatefulWidget {
  final EspecialidadeCatalogController controller;

  const EspecialidadesPage({super.key, required this.controller});

  @override
  State<EspecialidadesPage> createState() => _EspecialidadesPageState();
}

class _EspecialidadesPageState extends State<EspecialidadesPage> {
  final _searchController = TextEditingController();
  String _query = '';

  @override
  void initState() {
    super.initState();
    _searchController.addListener(
      () => setState(() => _query = _searchController.text),
    );
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Especialidades'),
      actions: [
        IconButton(
          tooltip: 'Atualizar catálogo',
          onPressed: () => widget.controller.load(refresh: true),
          icon: const Icon(Icons.refresh_rounded),
        ),
      ],
    ),
    body: AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final state = widget.controller.state;
        final filtered = _filter(state.items, _query);
        return RefreshIndicator(
          onRefresh: () => widget.controller.load(refresh: true),
          child: LayoutBuilder(
            builder: (context, constraints) => ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
              children: [
                Text(
                  'Encontre uma área de atendimento',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 8),
                const Text(
                  'Consulte o catálogo público de especialidades do AgendaiFisio.',
                ),
                const SizedBox(height: 20),
                TextField(
                  controller: _searchController,
                  textInputAction: TextInputAction.search,
                  decoration: InputDecoration(
                    labelText: 'Pesquisar especialidade',
                    prefixIcon: const Icon(Icons.search_rounded),
                    suffixIcon: _query.isEmpty
                        ? null
                        : IconButton(
                            tooltip: 'Limpar pesquisa',
                            onPressed: _searchController.clear,
                            icon: const Icon(Icons.clear_rounded),
                          ),
                  ),
                ),
                const SizedBox(height: 20),
                if (state.status == CatalogStatus.loading)
                  const SizedBox(height: 420, child: LoadingList())
                else if (state.status == CatalogStatus.error ||
                    state.status == CatalogStatus.offline)
                  SizedBox(
                    height: 360,
                    child: ErrorState(
                      message:
                          state.errorMessage ?? 'Sem conexão com o servidor.',
                      onRetry: widget.controller.load,
                    ),
                  )
                else if (state.status == CatalogStatus.empty ||
                    filtered.isEmpty)
                  const SizedBox(
                    height: 320,
                    child: EmptyState(
                      title: 'Nenhuma especialidade encontrada',
                      message: 'Tente outro termo de pesquisa.',
                    ),
                  )
                else
                  ...filtered.map((item) => _EspecialidadeCard(item: item)),
              ],
            ),
          ),
        );
      },
    ),
  );

  List<Especialidade> _filter(List<Especialidade> items, String query) {
    final normalized = query.trim().toLowerCase();
    if (normalized.isEmpty) return items;
    return items
        .where((item) => item.nome.toLowerCase().contains(normalized))
        .toList(growable: false);
  }
}

class _EspecialidadeCard extends StatelessWidget {
  final Especialidade item;

  const _EspecialidadeCard({required this.item});

  @override
  Widget build(BuildContext context) => Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: ListTile(
      minVerticalPadding: 12,
      leading: CircleAvatar(
        backgroundColor: Theme.of(context).colorScheme.primaryContainer,
        child: Icon(
          Icons.medical_services_outlined,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
      title: Text(item.nome),
      subtitle: const Text('Especialidade disponível no catálogo'),
    ),
  );
}
