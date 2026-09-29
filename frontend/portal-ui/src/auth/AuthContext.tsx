import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { AxiosError } from "axios";
import { apiClient, setSessionExpiredHandler, type ApiEnvelope } from "../api/client";
import { getAccessToken, setAccessToken } from "./tokenStore";
import type { CurrentUser, LoginResponseData } from "./authTypes";

interface LoginError {
  code: string;
  message: string;
}

interface AuthContextValue {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  /** True while the initial silent-refresh (on page load) is in flight. */
  isInitializing: boolean;
  login: (email: string, password: string) => Promise<{ success: true } | { success: false; error: LoginError }>;
  logout: () => Promise<void>;
  hasPermission: (permission: string) => boolean;
}

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setUser(null);
  }, []);

  // On first load, there's no access token in memory yet (page reload wipes it by
  // design — see tokenStore.ts) — silently try the refresh-token cookie before
  // deciding the user is signed out.
  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const refreshResponse = await apiClient.post<ApiEnvelope<{ accessToken: string }>>("/auth/refresh");
        const token = refreshResponse.data.data?.accessToken;
        if (!token) {
          throw new Error("No access token in refresh response");
        }
        setAccessToken(token);

        const meResponse = await apiClient.get<ApiEnvelope<CurrentUser>>("/auth/me");
        if (!cancelled) {
          setUser(meResponse.data.data ?? null);
        }
      } catch {
        if (!cancelled) {
          clearSession();
        }
      } finally {
        if (!cancelled) {
          setIsInitializing(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [clearSession]);

  useEffect(() => {
    setSessionExpiredHandler(clearSession);
    return () => setSessionExpiredHandler(null);
  }, [clearSession]);

  const login = useCallback(async (email: string, password: string) => {
    try {
      const response = await apiClient.post<ApiEnvelope<LoginResponseData>>("/auth/login", { email, password });
      const data = response.data.data!;
      setAccessToken(data.accessToken);
      setUser(data.user);
      return { success: true as const };
    } catch (err) {
      const axiosError = err as AxiosError<ApiEnvelope<unknown>>;
      const apiError = axiosError.response?.data?.error;
      return {
        success: false as const,
        error: apiError ?? { code: "UNKNOWN_ERROR", message: "Unable to sign in. Please try again." },
      };
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiClient.post("/auth/logout");
    } finally {
      clearSession();
    }
  }, [clearSession]);

  const hasPermission = useCallback((permission: string) => user?.permissions.includes(permission) ?? false, [user]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null && getAccessToken() !== null,
      isInitializing,
      login,
      logout,
      hasPermission,
    }),
    [user, isInitializing, login, logout, hasPermission],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
