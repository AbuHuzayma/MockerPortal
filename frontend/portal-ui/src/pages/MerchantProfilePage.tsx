import { useState } from "react";
import { Navigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useMerchantContext } from "../merchants/useMerchantContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "MERCHANT_DETAILS";

export function MerchantProfilePage() {
  const { merchantId } = useMerchantContext();
  const queryClient = useQueryClient();
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const merchantQuery = useQuery({
    queryKey: ["merchant", merchantId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/merchants/${merchantId}`);
      return response.data.data!;
    },
    enabled: !!merchantId,
  });

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const response = await apiClient.put<ApiEnvelope<FormValues>>(`/merchants/${merchantId}`, values);
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["merchant", merchantId], data);
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

  if (!merchantId) {
    return <Navigate to="/merchants" replace />;
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Merchant Details
        </Typography>

        {merchantQuery.isLoading && <CircularProgress size={28} />}
        {merchantQuery.isError && <Alert severity="error">Unable to load this merchant's details.</Alert>}

        {saveError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setSaveError(null)}>
            {saveError}
          </Alert>
        )}

        {merchantQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={merchantQuery.data}
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
        message="Merchant details saved"
      />
    </Card>
  );
}
