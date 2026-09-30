import { Outlet } from "react-router-dom";
import { Alert, Stack } from "@mui/material";
import { MerchantLookupBar } from "./MerchantLookupBar";
import { useMerchantContext } from "./useMerchantContext";

/** Merchant counterpart of CustomerWorkspace: lookup bar on top, screen remounted per merchant. */
export function MerchantWorkspace() {
  const { merchantId } = useMerchantContext();

  return (
    <Stack spacing={2}>
      <MerchantLookupBar />
      {merchantId ? (
        <Outlet key={merchantId} />
      ) : (
        <Alert severity="info">Search for a merchant by name above to open this screen.</Alert>
      )}
    </Stack>
  );
}
