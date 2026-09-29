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
  List,
  ListItemButton,
  ListItemText,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/SearchOutlined";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useMerchantContext } from "../merchants/useMerchantContext";

interface Merchant {
  merchantId: string;
  merchantNumber: string;
  nameEn: string | null;
  nameAr: string | null;
}

const schema = z.object({
  name: z.string().min(1, "A name is required"),
});

type FormValues = z.infer<typeof schema>;

export function MerchantSearchPage() {
  const navigate = useNavigate();
  const { setMerchantId } = useMerchantContext();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const searchMutation = useMutation({
    mutationFn: async (name: string) => {
      const response = await apiClient.get<ApiEnvelope<Merchant[]>>("/merchants/search", { params: { name } });
      return response.data.data!;
    },
  });

  const onSubmit = (values: FormValues) => searchMutation.mutate(values.name);

  const handleSelect = (merchantId: string) => {
    setMerchantId(merchantId);
    navigate("/merchants/profile");
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
            Merchant Search
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Search by name (English or Arabic, partial match).
          </Typography>

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack direction="row" spacing={2} sx={{ alignItems: "flex-start" }}>
              <TextField
                label="Merchant name"
                placeholder="e.g. Falcon"
                error={!!errors.name}
                helperText={errors.name?.message}
                sx={{ minWidth: 280 }}
                {...register("name")}
              />
              <Button type="submit" variant="contained" startIcon={<SearchIcon />} disabled={searchMutation.isPending} sx={{ height: 56 }}>
                {searchMutation.isPending ? "Searching…" : "Search"}
              </Button>
            </Stack>
          </Box>
        </CardContent>
      </Card>

      {errorMessage && <Alert severity="warning">{errorMessage}</Alert>}

      {searchMutation.isSuccess && searchMutation.data && (
        <Card>
          <CardContent>
            <Typography variant="h4" gutterBottom>
              Results
            </Typography>
            <Divider sx={{ mb: 1 }} />
            <List>
              {searchMutation.data.map((merchant) => (
                <ListItemButton key={merchant.merchantId} onClick={() => handleSelect(merchant.merchantId)}>
                  <ListItemText
                    primary={merchant.nameEn ?? merchant.nameAr ?? merchant.merchantId}
                    secondary={`Merchant ID: ${merchant.merchantId}`}
                  />
                </ListItemButton>
              ))}
            </List>
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}
