import { Outlet } from "react-router-dom";
import { Alert, Stack } from "@mui/material";
import { CustomerLookupBar } from "./CustomerLookupBar";
import { useCustomerContext } from "./useCustomerContext";

/**
 * Layout route for every screen that operates on a single customer. Screens are
 * only rendered once a customer is selected, and are remounted (keyed by
 * customer ID) on a switch so no form or dialog state carries over.
 */
export function CustomerWorkspace() {
  const { customer } = useCustomerContext();

  return (
    <Stack spacing={2}>
      <CustomerLookupBar />
      {customer ? (
        <Outlet key={customer.customerId} />
      ) : (
        <Alert severity="info">Search for a customer by mobile number above to open this screen.</Alert>
      )}
    </Stack>
  );
}
