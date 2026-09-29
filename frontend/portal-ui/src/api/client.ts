import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { getAccessToken, setAccessToken } from "../auth/tokenStore";

declare global {
  interface Window {
    /** Written by docker-entrypoint.d/env-config.sh at container startup — see index.html. */
    __ENV__?: { VITE_API_BASE_URL?: string };
  }
}

const baseURL = window.__ENV__?.VITE_API_BASE_URL || import.meta.env.VITE_API_BASE_URL || "/api/v1";

export const apiClient = axios.create({
  baseURL,
  withCredentials: true, // carries the httpOnly refresh-token cookie — docs/04 §1
});

/** Standard success/error envelope every backend response uses. See docs/06-api-design.md. */
export interface ApiEnvelope<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: unknown;
  };
  correlationId: string;
}

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.set("Authorization", `Bearer ${token}`);
  }
  return config;
});

let onSessionExpired: (() => void) | null = null;

/** AuthProvider registers this so an unrecoverable 401 can clear state and redirect to /login. */
export function setSessionExpiredHandler(handler: (() => void) | null): void {
  onSessionExpired = handler;
}

let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  refreshPromise ??= axios
    .post<ApiEnvelope<{ accessToken: string }>>(
      `${baseURL}/auth/refresh`,
      null,
      { withCredentials: true },
    )
    .then((response) => {
      const token = response.data.data?.accessToken ?? null;
      setAccessToken(token);
      return token;
    })
    .catch(() => {
      setAccessToken(null);
      return null;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    const isAuthEndpoint = originalRequest?.url?.includes("/auth/login") || originalRequest?.url?.includes("/auth/refresh");

    if (error.response?.status === 401 && originalRequest && !originalRequest._retried && !isAuthEndpoint) {
      originalRequest._retried = true;
      const newToken = await refreshAccessToken();

      if (newToken) {
        originalRequest.headers.set("Authorization", `Bearer ${newToken}`);
        return apiClient(originalRequest);
      }

      onSessionExpired?.();
    }

    return Promise.reject(error);
  },
);
