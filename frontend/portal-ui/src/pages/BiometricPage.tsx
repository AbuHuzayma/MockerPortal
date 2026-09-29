import { Navigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_BIOMETRIC";

export function BiometricPage() {
  const { customer } = useCustomerContext();

  const biometricQuery = useQuery({
    queryKey: ["biometric", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/biometrics`);
      return response.data.data!;
    },
    enabled: !!customer,
  });

  if (!customer) {
    return <Navigate to="/customers/search" replace />;
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Biometric Expiry
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber}
        </Typography>

        <Alert severity="info" sx={{ mb: 3 }}>
          No fields are configured yet — the Biometric Authentication
          Module's database schema has not been supplied (see
          docs/05-database-design.md §3). The screen, API, and provider
          abstraction are fully wired and ready to receive real fields.
        </Alert>

        {biometricQuery.isLoading && <CircularProgress size={28} />}
        {biometricQuery.isError && <Alert severity="error">Unable to load biometric details for this customer.</Alert>}

        {biometricQuery.data && (
          <DynamicForm screenCode={SCREEN_CODE} values={biometricQuery.data} onSubmit={async () => {}} />
        )}
      </CardContent>
    </Card>
  );
}
