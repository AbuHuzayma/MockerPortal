import { Navigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_ONBOARDING";

export function OnboardingPage() {
  const { customer } = useCustomerContext();

  const onboardingQuery = useQuery({
    queryKey: ["onboarding", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/onboarding`);
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
          Onboarding
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber} · Read-only.
        </Typography>

        {onboardingQuery.isLoading && <CircularProgress size={28} />}
        {onboardingQuery.isError && <Alert severity="error">Unable to load onboarding details for this customer.</Alert>}

        {onboardingQuery.data && (
          <DynamicForm screenCode={SCREEN_CODE} values={onboardingQuery.data} onSubmit={async () => {}} />
        )}
      </CardContent>
    </Card>
  );
}
