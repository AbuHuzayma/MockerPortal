import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useAuth } from "../auth/useAuth";

interface MerchantContextValue {
  merchantId: string | null;
  setMerchantId: (merchantId: string) => void;
  clearMerchant: () => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const MerchantContext = createContext<MerchantContextValue | null>(null);

/** Mirrors CustomerContext — in-memory only, cleared on logout, not persisted across a reload. */
export function MerchantProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [merchantId, setMerchantIdState] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      setMerchantIdState(null);
    }
  }, [isAuthenticated]);

  const setMerchantId = useCallback((id: string) => setMerchantIdState(id), []);
  const clearMerchant = useCallback(() => setMerchantIdState(null), []);

  const value = useMemo<MerchantContextValue>(
    () => ({ merchantId, setMerchantId, clearMerchant }),
    [merchantId, setMerchantId, clearMerchant],
  );

  return <MerchantContext.Provider value={value}>{children}</MerchantContext.Provider>;
}
