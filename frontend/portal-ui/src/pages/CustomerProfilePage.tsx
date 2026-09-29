import { Navigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, Stack, Grid, Divider, Chip, CircularProgress, Alert } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import type { Customer } from "../customers/customerTypes";

function Field({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value?.trim() ? value : "—"}</Typography>
    </Grid>
  );
}

export function CustomerProfilePage() {
  const { customer: context } = useCustomerContext();

  const query = useQuery({
    queryKey: ["customer-profile", context?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<Customer>>(`/customers/${context!.customerId}`);
      return response.data.data!;
    },
    enabled: !!context,
  });

  if (!context) {
    return <Navigate to="/customers/search" replace />;
  }

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ mb: 2, justifyContent: "space-between", alignItems: "flex-start" }}>
          <Typography variant="h3">Customer Profile</Typography>
          {query.data?.lifeStatus && <Chip label={query.data.lifeStatus} color="info" size="small" />}
        </Stack>

        {query.isLoading && <CircularProgress size={28} />}
        {query.isError && <Alert severity="error">Unable to load this customer's profile.</Alert>}

        {query.data && (
          <>
            <Divider sx={{ mb: 2 }} />
            <Grid container spacing={2}>
              <Field label="Customer ID" value={query.data.custId} />
              <Field label="Customer Number" value={query.data.custNumber} />
              <Field label="T24 Customer ID" value={query.data.t24CustomerId} />
              <Field label="Mobile Number" value={query.data.mobileNo} />
              <Field label="First Name" value={query.data.firstName} />
              <Field label="Last Name" value={query.data.lastName} />
              <Field label="Arabic First Name" value={query.data.arabicFirstName} />
              <Field label="Arabic Last Name" value={query.data.arabicLastName} />
              <Field label="Email" value={query.data.email} />
              <Field label="Nationality" value={query.data.nationality} />
              <Field label="Life Status" value={query.data.lifeStatus} />
              <Field label="Blacklist Status" value={query.data.blacklistStatus} />
            </Grid>
          </>
        )}
      </CardContent>
    </Card>
  );
}
