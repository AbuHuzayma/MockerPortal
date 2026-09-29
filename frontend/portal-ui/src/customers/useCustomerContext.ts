import { useContext } from "react";
import { CustomerContext } from "./CustomerContext";

export function useCustomerContext() {
  const context = useContext(CustomerContext);
  if (!context) {
    throw new Error("useCustomerContext must be used within a CustomerProvider");
  }
  return context;
}
