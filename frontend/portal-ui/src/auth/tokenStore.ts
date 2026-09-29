/**
 * Holds the access token in memory only (never localStorage/sessionStorage) so it
 * cannot be read by an XSS payload that survives a page reload — see
 * docs/04-authentication-authorization.md §5. The axios interceptor in api/client.ts
 * reads this directly; AuthContext is the only writer.
 */
let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}
