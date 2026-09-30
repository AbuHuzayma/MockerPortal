import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Alert, Box, Button, Card, CardContent, Chip, Stack, TextField, Typography } from "@mui/material";
import SearchIcon from "@mui/icons-material/SearchOutlined";
import PersonIcon from "@mui/icons-material/PersonOutlined";
import { useCustomerContext } from "./useCustomerContext";
import { toCustomerContext } from "./customerTypes";
import { customerSearchSchema, useCustomerSearch, type CustomerSearchValues } from "./useCustomerSearch";

/**
 * Shown above every customer screen: displays the active customer and lets the
 * user search for (or switch to) another one without leaving the screen.
 */
export function CustomerLookupBar() {
  const { customer, setCustomer } = useCustomerContext();
  const search = useCustomerSearch();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CustomerSearchValues>({ resolver: zodResolver(customerSearchSchema) });

  const onSubmit = (values: CustomerSearchValues) =>
    search.mutate(values.mobileNumber, {
      onSuccess: (found) => {
        setCustomer(toCustomerContext(found));
        reset();
      },
    });

  return (
    <Card>
      <CardContent>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={2}
          sx={{ alignItems: { md: "flex-start" }, justifyContent: "space-between" }}
        >
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Active customer
            </Typography>
            {customer ? (
              <Chip
                icon={<PersonIcon />}
                color="primary"
                label={`${customer.displayName ?? "Unnamed"} · ${customer.mobileNumber} · ${customer.customerId}`}
              />
            ) : (
              <Typography variant="body1">None selected</Typography>
            )}
          </Box>

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack direction="row" spacing={1} sx={{ alignItems: "flex-start" }}>
              <TextField
                size="small"
                label={customer ? "Switch customer (mobile)" : "Customer mobile number"}
                placeholder="e.g. 0500000001"
                error={!!errors.mobileNumber}
                helperText={errors.mobileNumber?.message}
                sx={{ minWidth: 240 }}
                {...register("mobileNumber")}
              />
              <Button type="submit" variant="contained" startIcon={<SearchIcon />} disabled={search.isPending}>
                {search.isPending ? "Searching…" : "Search"}
              </Button>
            </Stack>
          </Box>
        </Stack>

        {search.errorMessage && (
          <Alert severity="warning" sx={{ mt: 2 }} onClose={() => search.reset()}>
            {search.errorMessage}
          </Alert>
        )}
      </CardContent>
    </Card>
  );
}
