import { useState } from "react";
import { Navigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useMerchantContext } from "../merchants/useMerchantContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "MERCHANT_B2B";

export function MerchantB2BPage() {
  const { merchantId } = useMerchantContext();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const statusQuery = useQuery({
    queryKey: ["merchant-b2b", merchantId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/merchants/${merchantId}/b2b`);
      return response.data.data!;
    },
    enabled: !!merchantId,
  });

  const addMutation = useMutation({
    mutationFn: async () => {
      const response = await apiClient.post<ApiEnvelope<FormValues>>(`/merchants/${merchantId}/b2b`);
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["merchant-b2b", merchantId], data);
      setError(null);
      setSuccess(true);
    },
    onError: (err: unknown) => {
      const message =
        (err as { response?: { data?: ApiEnvelope<unknown> } })?.response?.data?.error?.message ??
        "Failed to add the merchant to B2B. Please try again.";
      setError(message);
    },
  });

  if (!merchantId) {
    return <Navigate to="/merchants" replace />;
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Add Merchant to B2B
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Via an internal B2B Subscription Matrix API (assumed contract — no real API spec supplied yet).
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {statusQuery.isLoading && <CircularProgress size={28} />}
        {statusQuery.isError && <Alert severity="error">Unable to load B2B status for this merchant.</Alert>}

        {statusQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={statusQuery.data}
            onSubmit={async () => {
              await addMutation.mutateAsync();
            }}
            isSaving={addMutation.isPending}
          />
        )}
      </CardContent>

      <Snackbar open={success} autoHideDuration={3000} onClose={() => setSuccess(false)} message="Added to B2B" />
    </Card>
  );
}
