import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DEFAULT_BENEFICIARY_ID } from "../customers/customerTypes";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_BENEFICIARY";

export function BeneficiaryPage() {
  const { customer } = useCustomerContext();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const statusQuery = useQuery({
    queryKey: ["beneficiary", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(
        `/customers/${customer!.customerId}/beneficiaries/${DEFAULT_BENEFICIARY_ID}`,
      );
      return response.data.data!;
    },
    enabled: !!customer,
  });

  const activateMutation = useMutation({
    mutationFn: async () => {
      const response = await apiClient.post<ApiEnvelope<FormValues>>(
        `/customers/${customer!.customerId}/beneficiaries/${DEFAULT_BENEFICIARY_ID}/activate`,
      );
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["beneficiary", customer?.customerId], data);
      setError(null);
      setSuccess(true);
    },
    onError: (err: unknown) => {
      const message =
        (err as { response?: { data?: ApiEnvelope<unknown> } })?.response?.data?.error?.message ??
        "Failed to activate the beneficiary. Please try again.";
      setError(message);
    },
  });

  if (!customer) {
    return null; // CustomerWorkspace only renders this screen once a customer is selected.
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Beneficiary Activation
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber} · Via an internal Beneficiary Service API
          (assumed contract — no real API spec has been supplied yet, and there is no
          "list beneficiaries" data yet, so this shows the one default mock beneficiary).
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {statusQuery.isLoading && <CircularProgress size={28} />}
        {statusQuery.isError && <Alert severity="error">Unable to load beneficiary status for this customer.</Alert>}

        {statusQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={statusQuery.data}
            onSubmit={async () => {
              await activateMutation.mutateAsync();
            }}
            isSaving={activateMutation.isPending}
          />
        )}
      </CardContent>

      <Snackbar open={success} autoHideDuration={3000} onClose={() => setSuccess(false)} message="Beneficiary activated" />
    </Card>
  );
}
