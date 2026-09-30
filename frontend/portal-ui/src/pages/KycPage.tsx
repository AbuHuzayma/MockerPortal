import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_KYC";

export function KycPage() {
  const { customer } = useCustomerContext();
  const queryClient = useQueryClient();
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const kycQuery = useQuery({
    queryKey: ["kyc", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/kyc`);
      return response.data.data!;
    },
    enabled: !!customer,
  });

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const response = await apiClient.put<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/kyc`, values);
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["kyc", customer?.customerId], data);
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
    return null; // CustomerWorkspace only renders this screen once a customer is selected.
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Update KYC Details
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber} · Relational sections (residency, contact person) are not yet
          implemented — pending schema, see docs/05-database-design.md §4.
        </Typography>

        {kycQuery.isLoading && <CircularProgress size={28} />}
        {kycQuery.isError && <Alert severity="error">Unable to load KYC details for this customer.</Alert>}

        {saveError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setSaveError(null)}>
            {saveError}
          </Alert>
        )}

        {kycQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={kycQuery.data}
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
        message="KYC details saved"
      />
    </Card>
  );
}
