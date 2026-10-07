import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

test("Scalar gerencia automaticamente a sessão JWT", async () => {
  const source = await readFile(
    new URL("../../wwwroot/scalar-config.js", import.meta.url),
    "utf8",
  );
  const storage = new Map();
  const requests = [];
  let protectedStatus = 200;

  globalThis.window = {
    location: { origin: "http://localhost:5048" },
    sessionStorage: {
      getItem: (key) => storage.get(key) ?? null,
      setItem: (key, value) => storage.set(key, value),
      removeItem: (key) => storage.delete(key),
    },
    fetch: async (input, init = {}) => {
      const url = new URL(
        input instanceof Request ? input.url : input,
        window.location.origin,
      );
      const authorization = new Headers(init.headers).get("Authorization");
      requests.push({ path: url.pathname, origin: url.origin, authorization });

      if (url.pathname === "/api/auth/login") {
        return Response.json({ accessToken: "jwt-da-sessao" });
      }

      if (url.pathname === "/api/auth/logout") {
        return new Response(null, { status: 204 });
      }

      const status = url.pathname === "/api/Agendamento" ? protectedStatus : 200;
      return Response.json({}, { status });
    },
  };

  const moduleUrl = `data:text/javascript;base64,${Buffer.from(source).toString("base64")}`;
  const { default: config } = await import(moduleUrl);
  const tokenKey = "agendai.scalar.access-token";

  await config.customFetch("/api/auth/login", {
    method: "POST",
    headers: { Authorization: "Bearer token-antigo" },
  });
  assert.equal(requests.at(-1).authorization, null, "login não recebe Bearer");
  assert.equal(storage.get(tokenKey), "jwt-da-sessao", "login salva accessToken");

  await config.customFetch("/api/Agendamento");
  assert.equal(
    requests.at(-1).authorization,
    "Bearer jwt-da-sessao",
    "rota protegida recebe o JWT sem cópia manual",
  );

  await config.customFetch("/api/auth/register", {
    headers: { Authorization: "Bearer manual" },
  });
  assert.equal(requests.at(-1).authorization, null, "cadastro não recebe Bearer");

  await config.customFetch("/openapi/v1.json", {
    headers: { Authorization: "Bearer manual" },
  });
  assert.equal(requests.at(-1).authorization, null, "OpenAPI não recebe Bearer");

  await config.customFetch("https://api.example/recurso");
  assert.equal(requests.at(-1).authorization, null, "JWT não vaza para outra origem");

  protectedStatus = 401;
  await config.customFetch("/api/Agendamento");
  assert.equal(storage.has(tokenKey), false, "401 limpa a sessão");

  protectedStatus = 200;
  await config.customFetch("/api/auth/login", { method: "POST" });
  await config.customFetch("/api/auth/logout", { method: "POST" });
  assert.equal(storage.has(tokenKey), false, "logout limpa a sessão");
});
