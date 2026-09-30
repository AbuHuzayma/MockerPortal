import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, CircularProgress, Alert } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";

const SCREEN_CODE = "CUSTOMER_IVR";

export function IvrPage() {
  const { customer } = useCustomerContext();

  const ivrQuery = useQuery({
    queryKey: ["ivr", customer?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(`/customers/${customer!.customerId}/ivr`);
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
          Update IVR Details
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Customer: {customer.mobileNumber}
        </Typography>

        <Alert severity="info" sx={{ mb: 3 }}>
          No IVR fields are configured yet — the IVR database schema has not
          been supplied. The master spec explicitly calls for not inventing
          field names for this screen (see docs/05-database-design.md §3).
          The screen, API, and provider abstraction are fully wired and ready
          to receive real fields once the schema is available.
        </Alert>

        {ivrQuery.isLoading && <CircularProgress size={28} />}
        {ivrQuery.isError && <Alert severity="error">Unable to load IVR details for this customer.</Alert>}

        {ivrQuery.data && (
          <DynamicForm
            screenCode={SCREEN_CODE}
            values={ivrQuery.data}
            onSubmit={async () => {
              /* no-op: no SAVE action is registered while the field catalog is empty */
            }}
          />
        )}
      </CardContent>
    </Card>
  );
}
