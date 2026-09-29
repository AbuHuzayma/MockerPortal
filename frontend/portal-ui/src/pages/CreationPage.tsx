import { useState } from "react";
import { Navigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_CREATION";

export function CreationPage() {
  const { customer } = useCustomerContext();
  const queryClient = useQueryClient();
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const creationQuery = useQuery({
    queryKey: ["creation", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/creation`);
      return response.data.data!;
    },
    enabled: !!customer,
  });

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const response = await apiClient.put<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/creation`, values);
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["creation", customer?.customerId], data);
      setSaveError(null);
      setSaveSuccess(true);
    },
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: ApiEnvelope<unknown> } })?.response?.data?.error?.message ??
        "Save failed. Please try again.";
      setSaveError(message);
    },
  });

  if (!customer) {
    return <Navigate to="/customers/search" replace />;
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Customer Creation Details
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber} · Date of Birth and KYC Date map to one of two possible
          columns in the master spec (BIRTH_DATE/CUST_BIRTH_DATE, KYC_OK_DATE/KYC_LEVEL_DATE) —
          confirm against the real schema, see docs/05-database-design.md.
        </Typography>

        {creationQuery.isLoading && <CircularProgress size={28} />}
        {creationQuery.isError && <Alert severity="error">Unable to load creation details for this customer.</Alert>}

        {saveError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setSaveError(null)}>
            {saveError}
          </Alert>
        )}

        {creationQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={creationQuery.data}
            onSubmit={async (_actionCode, values) => {
              await saveMutation.mutateAsync(values);
            }}
            isSaving={saveMutation.isPending}
          />
        )}
      </CardContent>

      <Snackbar
        open={saveSuccess}
        autoHideDuration={3000}
        onClose={() => setSaveSuccess(false)}
        message="Creation details saved"
      />
    </Card>
  );
}
