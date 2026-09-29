import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation } from "@tanstack/react-query";
import { AxiosError } from "axios";
import { useNavigate } from "react-router-dom";
import {
  Card,
  CardContent,
  Typography,
  Stack,
  TextField,
  Button,
  Alert,
  Divider,
  Box,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/SearchOutlined";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useCustomerContext } from "../customers/useCustomerContext";
import { toCustomerContext, type Customer } from "../customers/customerTypes";

const schema = z.object({
  mobileNumber: z
    .string()
    .min(1, "Mobile number is required")
    .regex(/^[0-9]{7,15}$/, "Enter 7-15 digits, numbers only"),
});

type FormValues = z.infer<typeof schema>;

export function CustomerSearchPage() {
  const navigate = useNavigate();
  const { setCustomer } = useCustomerContext();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const searchMutation = useMutation({
    mutationFn: async (mobileNumber: string) => {
      const response = await apiClient.get<ApiEnvelope<Customer>>("/customers/search", {
        params: { mobileNumber },
      });
      return response.data.data!;
    },
  });

  const onSubmit = (values: FormValues) => searchMutation.mutate(values.mobileNumber);

  const handleViewProfile = (customer: Customer) => {
    setCustomer(toCustomerContext(customer));
    navigate("/customers/profile");
  };

  const errorMessage = searchMutation.isError
    ? ((searchMutation.error as AxiosError<ApiEnvelope<unknown>>).response?.data?.error?.message ??
      "Something went wrong while searching. Please try again.")
    : null;

  return (
    <Stack spacing={2}>
      <Card>
        <CardContent>
          <Typography variant="h3" gutterBottom>
            Customer Search
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Search by mobile number. Additional identifiers (Customer ID, National ID, ...) are planned.
          </Typography>

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack direction="row" spacing={2} sx={{ alignItems: "flex-start" }}>
              <TextField
                label="Mobile number"
                placeholder="e.g. 0500000001"
                error={!!errors.mobileNumber}
                helperText={errors.mobileNumber?.message}
                sx={{ minWidth: 280 }}
                {...register("mobileNumber")}
              />
              <Button
                type="submit"
                variant="contained"
                startIcon={<SearchIcon />}
                disabled={searchMutation.isPending}
                sx={{ height: 56 }}
              >
                {searchMutation.isPending ? "Searching…" : "Search"}
              </Button>
            </Stack>
          </Box>
        </CardContent>
      </Card>

      {errorMessage && <Alert severity={searchMutation.isError ? "warning" : "error"}>{errorMessage}</Alert>}

      {searchMutation.isSuccess && searchMutation.data && (
        <Card>
          <CardContent>
            <Typography variant="h4" gutterBottom>
              Result
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <Stack spacing={0.5} sx={{ mb: 2 }}>
              <Typography variant="body1">
                <strong>{[searchMutation.data.firstName, searchMutation.data.lastName].filter(Boolean).join(" ") || "—"}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Customer ID: {searchMutation.data.custId} · Mobile: {searchMutation.data.mobileNo}
              </Typography>
            </Stack>
            <Button variant="outlined" onClick={() => handleViewProfile(searchMutation.data)}>
              View Profile
            </Button>
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}
