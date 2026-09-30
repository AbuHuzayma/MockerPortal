import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_OTP";

export function OtpPage() {
  const { customer } = useCustomerContext();

  const otpQuery = useQuery({
    queryKey: ["otp-cooling", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/otp-cooling`);
      return response.data.data!;
    },
    enabled: !!customer,
  });

  if (!customer) {
    return null; // CustomerWorkspace only renders this screen once a customer is selected.
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          OTP / IVR Cooling Period
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber}
        </Typography>

        <Alert severity="info" sx={{ mb: 3 }}>
          No fields are configured yet — the master spec gives no field names
          for this screen, only "the backend must encapsulate the MSSQL
          implementation." The screen, API, and provider abstraction are
          fully wired and ready to receive real fields once the schema is
          available.
        </Alert>

        {otpQuery.isLoading && <CircularProgress size={28} />}
        {otpQuery.isError && <Alert severity="error">Unable to load OTP cooling details for this customer.</Alert>}

        {otpQuery.data && (
          <DynamicForm screenCode={SCREEN_CODE} values={otpQuery.data} onSubmit={async () => {}} />
        )}
      </CardContent>
    </Card>
  );
}
