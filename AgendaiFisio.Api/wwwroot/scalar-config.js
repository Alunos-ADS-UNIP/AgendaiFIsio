const accessTokenStorageKey = "agendai.scalar.access-token";

const anonymousPaths = new Set([
  "/api/auth/login",
  "/api/auth/register",
]);

const normalizePath = (pathname) => {
  const normalized = pathname.toLowerCase().replace(/\/+$/, "");
  return normalized || "/";
};

const getRequestUrl = (input) => {
  const value = input instanceof Request ? input.url : input;
  return new URL(value, window.location.origin);
};

const isOpenApiRequest = (pathname) =>
  pathname === "/openapi" || pathname.startsWith("/openapi/");

const isAnonymousRequest = (url) => {
  const pathname = normalizePath(url.pathname);
  return anonymousPaths.has(pathname) || isOpenApiRequest(pathname);
};

const isLoginRequest = (url) =>
  normalizePath(url.pathname) === "/api/auth/login";

const isLogoutRequest = (url) =>
  normalizePath(url.pathname) === "/api/auth/logout";

const getResponseAccessToken = async (response) => {
  try {
    const body = await response.clone().json();
    const token = body?.accessToken ?? body?.token ?? body?.Token;
    return typeof token === "string" && token.trim() ? token : null;
  } catch {
    return null;
  }
};

export default {
  customFetch: async (input, init = {}) => {
    const url = getRequestUrl(input);
    const headers = new Headers(input instanceof Request ? input.headers : undefined);

    new Headers(init.headers).forEach((value, key) => headers.set(key, value));

    if (isAnonymousRequest(url)) {
      // Mesmo que o painel nativo do Scalar contenha um token, estas rotas são anônimas.
      headers.delete("Authorization");
    } else if (url.origin === window.location.origin) {
      const token = window.sessionStorage.getItem(accessTokenStorageKey);

      if (token) {
        headers.set("Authorization", `Bearer ${token}`);
      }
    }

    const response = await window.fetch(input, { ...init, headers });

    if (isLoginRequest(url) && response.ok) {
      const token = await getResponseAccessToken(response);

      if (token) {
        window.sessionStorage.setItem(accessTokenStorageKey, token);
      }
    }

    if (response.status === 401 || (isLogoutRequest(url) && response.ok)) {
      window.sessionStorage.removeItem(accessTokenStorageKey);
    }

    return response;
  },
};
