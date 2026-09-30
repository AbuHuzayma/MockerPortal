import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useAuth } from "../auth/useAuth";

export interface SelectedMerchant {
  merchantId: string;
  displayName: string | null;
}

interface MerchantContextValue {
  merchant: SelectedMerchant | null;
  merchantId: string | null;
  setMerchant: (merchant: SelectedMerchant) => void;
  clearMerchant: () => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const MerchantContext = createContext<MerchantContextValue | null>(null);

/** Mirrors CustomerContext — in-memory only, cleared on logout, not persisted across a reload. */
export function MerchantProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [merchant, setMerchantState] = useState<SelectedMerchant | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      setMerchantState(null);
    }
  }, [isAuthenticated]);

  const setMerchant = useCallback((next: SelectedMerchant) => setMerchantState(next), []);
  const clearMerchant = useCallback(() => setMerchantState(null), []);

  const value = useMemo<MerchantContextValue>(
    () => ({ merchant, merchantId: merchant?.merchantId ?? null, setMerchant, clearMerchant }),
    [merchant, setMerchant, clearMerchant],
  );

  return <MerchantContext.Provider value={value}>{children}</MerchantContext.Provider>;
}
