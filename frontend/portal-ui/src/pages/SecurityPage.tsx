import { useState } from "react";
import { Navigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_SECURITY";

export function SecurityPage() {
  const { customer } = useCustomerContext();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const statusQuery = useQuery({
    queryKey: ["security", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/security`);
      return response.data.data!;
    },
    enabled: !!customer,
  });

  const removeLockMutation = useMutation({
    mutationFn: async () => {
      const response = await apiClient.post<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/security/remove-lock`);
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["security", customer?.customerId], data);
      setError(null);
      setSuccess(true);
    },
    onError: (err: unknown) => {
      const message =
        (err as { response?: { data?: ApiEnvelope<unknown> } })?.response?.data?.error?.message ??
        "Failed to remove the security lock. Please try again.";
      setError(message);
    },
  });

  if (!customer) {
    return <Navigate to="/customers/search" replace />;
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Remove Security Lock
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber} · PREPROD-sensitive — this action requires the
          customer.security.remove permission and is not granted to QA/Developer roles by default.
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {statusQuery.isLoading && <CircularProgress size={28} />}
        {statusQuery.isError && <Alert severity="error">Unable to load security status for this customer.</Alert>}

        {statusQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={statusQuery.data}
            onSubmit={async () => {
              await removeLockMutation.mutateAsync();
            }}
            isSaving={removeLockMutation.isPending}
          />
        )}
      </CardContent>

      <Snackbar
        open={success}
        autoHideDuration={3000}
        onClose={() => setSuccess(false)}
        message="Security lock removed"
      />
    </Card>
  );
}
