#!/usr/bin/env bash
set -euo pipefail

base_url="${1:-http://127.0.0.1:5187}"
suffix="$(date +%s)-$$"
passes=0
failures=0
last_body=""
last_status=""

request() {
  local method="$1"
  local path="$2"
  local token="${3:-}"
  local body="${4:-}"
  local body_file
  body_file="$(mktemp)"
  local args=(-sS -o "$body_file" -w '%{http_code}' -X "$method" "$base_url$path")
  if [[ -n "$token" ]]; then
    args+=(-H "Authorization: Bearer $token")
  fi
  if [[ -n "$body" ]]; then
    args+=(-H 'Content-Type: application/json' --data "$body")
  fi
  last_status="$(curl "${args[@]}")"
  last_body="$(<"$body_file")"
  rm -f "$body_file"
}

expect_status() {
  local expected="$1"
  local name="$2"
  if [[ "$last_status" == "$expected" ]]; then
    printf 'PASS | %-3s | %s\n' "$expected" "$name"
    passes=$((passes + 1))
  else
    printf 'FAIL | esperado %s, recebido %s | %s | %s\n' "$expected" "$last_status" "$name" "$last_body"
    failures=$((failures + 1))
  fi
}

expect_jq() {
  local expression="$1"
  local name="$2"
  if jq -e "$expression" >/dev/null 2>&1 <<<"$last_body"; then
    printf 'PASS | JSON | %s\n' "$name"
    passes=$((passes + 1))
  else
    printf 'FAIL | JSON | %s | %s\n' "$name" "$last_body"
    failures=$((failures + 1))
  fi
}

patient_pending_email="pr15-pending-$suffix@example.invalid"
patient_complete_email="pr15-complete-$suffix@example.invalid"
patient_invalid_data_email="pr15-invalid-data-$suffix@example.invalid"
professional_email="pr15-prof-$suffix@example.invalid"
professional_pending_email="pr15-prof-pending-$suffix@example.invalid"
weak_password_email="pr15-weak-password-$suffix@example.invalid"
password='Regression123!'
crefito="CR${suffix//-/}"

register() {
  local email="$1"
  local role="$2"
  request POST '/api/auth/register' '' "$(jq -nc --arg email "$email" --arg senha "$password" --arg role "$role" '{email:$email,senha:$senha,tipoUsuario:$role}')"
  expect_status 200 "cadastro de $role para auditoria"
}

login() {
  local email="$1"
  request POST '/api/auth/login' '' "$(jq -nc --arg email "$email" --arg senha "$password" '{email:$email,senha:$senha}')"
  expect_status 200 "login de $email" >&2
  jq -r '.accessToken' <<<"$last_body"
}

register "$patient_pending_email" Paciente
register "$patient_complete_email" Paciente
register "$patient_invalid_data_email" Paciente
register "$professional_email" Profissional
register "$professional_pending_email" Profissional

request POST '/api/auth/register' '' "$(jq -nc --arg email "$weak_password_email" '{email:$email,senha:"1234567",tipoUsuario:"Paciente"}')"
expect_status 400 'cadastro rejeita senha com menos de oito caracteres'

pending_patient_token="$(login "$patient_pending_email")"
complete_patient_token="$(login "$patient_complete_email")"
invalid_data_patient_token="$(login "$patient_invalid_data_email")"
professional_token="$(login "$professional_email")"
pending_professional_token="$(login "$professional_pending_email")"

for attempt in {1..6}; do
  request POST '/api/auth/login' '' "$(jq -nc --arg email "$patient_pending_email" '{email:$email,senha:"senha-incorreta"}')"
  expect_status 401 "tentativa inválida de login $attempt não sofre rate limit"
done

request POST '/api/auth/logout' "$pending_patient_token"
expect_status 204 'logout responde sem conteúdo'
request GET '/api/auth/me' "$pending_patient_token"
expect_status 200 'token continua válido depois do logout stateless'

request GET '/api/paciente/me' "$pending_patient_token"
expect_status 200 'perfil recém-criado do paciente é consultável'
expect_jq '.nomeCompleto == "Cadastro Pendente"' 'cadastro inicial ainda grava o texto Cadastro Pendente no NomeCompleto'

request GET '/api/auth/me' "$professional_token"
professional_id="$(jq -r '.perfilId' <<<"$last_body")"
request GET '/api/auth/me' "$pending_professional_token"
pending_professional_id="$(jq -r '.perfilId' <<<"$last_body")"

request PUT '/api/profissional/completar-perfil' "$professional_token" "$(jq -nc --arg crefito "$crefito" '{nomeCompleto:"Profissional Auditoria",cpf:"12345678909",crefito:$crefito,telefone:"11999999999",dataNascimento:"1990-01-01",bio:"Auditoria PR 15",ativo:true}')"
expect_status 200 'profissional completa o perfil'

request PUT '/api/paciente/completar-perfil' "$complete_patient_token" '{"nomeCompleto":"Paciente Completo","cpf":"52998224725","telefone":"11988887777","dataNascimento":"1992-02-02","sexo":"","estadoCivil":"","rua":"Rua Teste","numero":"10","cep":"01001000","complemento":"","bairro":"Centro","cidade":"São Paulo","estado":"SP"}'
expect_status 200 'paciente completa todos os campos exigidos pelo agendamento'

request GET '/api/profissional/me'
expect_status 401 'profissional/me rejeita anônimo'
request GET '/api/profissional/me' "$pending_patient_token"
expect_status 403 'profissional/me rejeita paciente'
request GET '/api/profissional/me' "$professional_token"
expect_status 200 'profissional/me aceita o próprio profissional'
expect_jq '.id == "'"$professional_id"'" and .cpf == "12345678909" and .dataNascimento != null and .email == "'"$professional_email"'"' 'profissional/me devolve o perfil correto e os campos privados necessários'
expect_jq 'has("senha") | not' 'profissional/me não expõe senha'

request GET "/api/profissional/$professional_id" "$pending_patient_token"
expect_status 200 'detalhe público do profissional continua acessível ao paciente autenticado'
expect_jq '(has("cpf") or has("dataNascimento")) | not' 'detalhe público não expõe CPF nem nascimento'

request GET '/api/profissional?ativo=true&pagina=1&tamanhoPagina=50' "$pending_patient_token"
expect_status 200 'listagem autenticada de profissionais ativos'
expect_jq '[.itens[] | select(.id == "'"$pending_professional_id"'" and .nomeCompleto == "Cadastro Pendente" and .ativo == true)] | length == 1' 'profissional pendente aparece como ativo e agendável na listagem'

future_day="$(date -u -v+10d '+%Y-%m-%d')"
range_start="${future_day}T00:00:00Z"
range_end="${future_day}T23:59:59Z"
block_at="${future_day}T10:00:00Z"
appointment_at="${future_day}T12:00:00Z"
pending_professional_appointment_at="${future_day}T14:00:00Z"
invalid_data_appointment_at="${future_day}T15:00:00Z"

encoded_range="?inicio=${range_start//:/%3A}&fim=${range_end//:/%3A}"
request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range"
expect_status 401 'indisponibilidade rejeita anônimo'
request GET "/api/profissional/$professional_id/indisponibilidade" "$pending_patient_token"
expect_status 400 'indisponibilidade exige início e fim'
request GET "/api/profissional/$professional_id/indisponibilidade?inicio=${range_start//:/%3A}&fim=${range_start//:/%3A}" "$pending_patient_token"
expect_status 400 'indisponibilidade rejeita fim igual ao início'
request GET "/api/profissional/$professional_id/indisponibilidade?inicio=2026-11-01T00%3A00%3A00Z&fim=2027-03-01T00%3A00%3A00Z" "$pending_patient_token"
expect_status 400 'indisponibilidade limita o intervalo a 93 dias'
request GET "/api/profissional/00000000-0000-0000-0000-000000000001/indisponibilidade$encoded_range" "$pending_patient_token"
expect_status 404 'indisponibilidade de profissional inexistente retorna 404'
request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range" "$pending_patient_token"
expect_status 200 'paciente autenticado consulta indisponibilidade'
request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range" "$professional_token"
expect_status 200 'profissional autenticado consulta indisponibilidade'
request GET "/api/profissional/$pending_professional_id/indisponibilidade$encoded_range" "$complete_patient_token"
expect_status 200 'profissional com cadastro pendente é tratado como disponível porque nasce ativo'

request POST '/api/agendamento' "$complete_patient_token" "$(jq -nc --arg id "$pending_professional_id" --arg data "$pending_professional_appointment_at" '{profissionalId:$id,dataHora:$data}')"
expect_status 201 'paciente consegue agendar com profissional que nunca completou o cadastro'
pending_professional_appointment_id="$(jq -r '.id' <<<"$last_body")"

request PUT '/api/paciente/completar-perfil' "$invalid_data_patient_token" '{"nomeCompleto":"Paciente Dados Inválidos","cpf":"39053344705","telefone":"x","dataNascimento":"2099-01-01","sexo":"","estadoCivil":"","rua":"x","numero":"x","cep":"x","complemento":"","bairro":"x","cidade":"x","estado":"x"}'
expect_status 200 'completar-perfil aceita nascimento futuro, telefone e endereço sem formato válido'
request POST '/api/agendamento' "$invalid_data_patient_token" "$(jq -nc --arg id "$professional_id" --arg data "$invalid_data_appointment_at" '{profissionalId:$id,dataHora:$data}')"
expect_status 201 'validação de cadastro considera os dados semanticamente inválidos como completos'
invalid_data_appointment_id="$(jq -r '.id' <<<"$last_body")"

request POST '/api/agenda/bloqueios/horarios' "$professional_token" "$(jq -nc --arg data "$block_at" '{dataHora:$data,motivo:"Motivo privado PR15"}')"
expect_status 201 'profissional cria bloqueio para validar a rota agregada'
block_id="$(jq -r '.id' <<<"$last_body")"

request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range" "$pending_patient_token"
expect_status 200 'paciente recebe o bloqueio como indisponibilidade'
expect_jq 'any(.[]; .inicio == "'"$block_at"'" and .fim == "'"${future_day}T11:00:00Z"'")' 'bloqueio é devolvido como intervalo de uma hora'
expect_jq 'all(.[]; (keys | sort) == ["fim","inicio"])' 'indisponibilidade não vaza id, motivo ou dados de paciente'

request POST '/api/agendamento' "$pending_patient_token" "$(jq -nc --arg id "$professional_id" --arg data "$appointment_at" '{profissionalId:$id,dataHora:$data}')"
expect_status 422 'paciente pendente não pode criar agendamento'
expect_jq '.erro | test("Complete seu cadastro")' '422 explica como liberar o agendamento'

request POST '/api/agendamento' "$complete_patient_token" "$(jq -nc --arg id "$professional_id" --arg data "$appointment_at" '{profissionalId:$id,dataHora:$data}')"
expect_status 201 'paciente completo cria agendamento'
appointment_id="$(jq -r '.id' <<<"$last_body")"

request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range" "$pending_patient_token"
expect_status 200 'indisponibilidade agrega bloqueios e consultas'
expect_jq 'any(.[]; .inicio == "'"$block_at"'") and any(.[]; .inicio == "'"$appointment_at"'" and .fim == "'"${future_day}T13:00:00Z"'")' 'consulta ocupa exatamente uma hora na resposta'

request PATCH "/api/agendamento/$appointment_id/cancelar" "$complete_patient_token"
expect_status 200 'paciente cancela a consulta usada na auditoria'
request PATCH "/api/agendamento/$pending_professional_appointment_id/cancelar" "$complete_patient_token"
expect_status 200 'limpeza da consulta criada com profissional pendente'
request PATCH "/api/agendamento/$invalid_data_appointment_id/cancelar" "$invalid_data_patient_token"
expect_status 200 'limpeza da consulta criada com dados semanticamente inválidos'
request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range" "$pending_patient_token"
expect_status 200 'indisponibilidade após cancelamento'
expect_jq 'length == 1 and .[0].inicio == "'"$block_at"'"' 'consulta cancelada deixa de bloquear a agenda'

request GET "/api/profissional/$professional_id/indisponibilidade?inicio=${future_day}T11%3A00%3A00Z&fim=${future_day}T12%3A00%3A00Z" "$pending_patient_token"
expect_status 200 'consulta de fronteira entre intervalos'
expect_jq 'length == 0' 'bloqueio que termina exatamente no início não é retornado'

request PUT '/api/profissional/completar-perfil' "$professional_token" "$(jq -nc --arg crefito "$crefito" '{nomeCompleto:"Profissional Auditoria",cpf:"12345678909",crefito:$crefito,telefone:"11999999999",dataNascimento:"1990-01-01",bio:"Auditoria PR 15",ativo:false}')"
expect_status 200 'profissional pode ser marcado como inativo pelo próprio perfil'
request GET "/api/profissional/$professional_id/indisponibilidade$encoded_range" "$pending_patient_token"
expect_status 404 'indisponibilidade trata profissional inativo como inexistente'

request DELETE "/api/agenda/bloqueios/$block_id" "$professional_token"
expect_status 204 'limpeza do bloqueio criado pela auditoria'

printf '\nTOTAL: %d aprovadas, %d falhas\n' "$passes" "$failures"
(( failures == 0 ))
