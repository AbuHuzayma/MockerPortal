import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useAuth } from "../auth/useAuth";
import type { CustomerContextValue } from "./customerTypes";

interface CustomerContextState {
  customer: CustomerContextValue | null;
  setCustomer: (customer: CustomerContextValue) => void;
  clearCustomer: () => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const CustomerContext = createContext<CustomerContextState | null>(null);

/**
 * Holds the "active customer" identifiers (master spec §9) for the duration of a
 * customer's screens — set once by the Search page, read by every screen under
 * /customers/*. Deliberately in-memory only (not persisted across a reload):
 * a reload should re-search, not silently resume an old context.
 */
export function CustomerProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [customer, setCustomerState] = useState<CustomerContextValue | null>(null);

  // A logout should never leave a stale customer context behind for the next sign-in.
  useEffect(() => {
    if (!isAuthenticated) {
      setCustomerState(null);
    }
  }, [isAuthenticated]);

  const setCustomer = useCallback((next: CustomerContextValue) => setCustomerState(next), []);
  const clearCustomer = useCallback(() => setCustomerState(null), []);

  const value = useMemo<CustomerContextState>(
    () => ({ customer, setCustomer, clearCustomer }),
    [customer, setCustomer, clearCustomer],
  );

  return <CustomerContext.Provider value={value}>{children}</CustomerContext.Provider>;
}
