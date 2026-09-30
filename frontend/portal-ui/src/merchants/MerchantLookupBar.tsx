import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation } from "@tanstack/react-query";
import { AxiosError } from "axios";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/SearchOutlined";
import StorefrontIcon from "@mui/icons-material/StorefrontOutlined";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useMerchantContext } from "./useMerchantContext";

interface MerchantSearchResult {
  merchantId: string;
  merchantNumber: string;
  nameEn: string | null;
  nameAr: string | null;
}

const schema = z.object({
  name: z.string().min(1, "A name is required"),
});

type SearchValues = z.infer<typeof schema>;

/**
 * Shown above every merchant screen: displays the active merchant and lets the
 * user search by name (partial match) and pick another without leaving the screen.
 */
export function MerchantLookupBar() {
  const { merchant, setMerchant } = useMerchantContext();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SearchValues>({ resolver: zodResolver(schema) });

  const search = useMutation({
    mutationFn: async (name: string) => {
      const response = await apiClient.get<ApiEnvelope<MerchantSearchResult[]>>("/merchants/search", {
        params: { name },
      });
      return response.data.data!;
    },
  });

  const errorMessage = search.isError
    ? ((search.error as AxiosError<ApiEnvelope<unknown>>).response?.data?.error?.message ??
      "Something went wrong while searching. Please try again.")
    : null;

  const select = (result: MerchantSearchResult) => {
    setMerchant({ merchantId: result.merchantId, displayName: result.nameEn ?? result.nameAr });
    search.reset();
    reset();
  };

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
              Active merchant
            </Typography>
            {merchant ? (
              <Chip
                icon={<StorefrontIcon />}
                color="primary"
                label={`${merchant.displayName ?? "Unnamed"} · ${merchant.merchantId}`}
              />
            ) : (
              <Typography variant="body1">None selected</Typography>
            )}
          </Box>

          <Box component="form" onSubmit={handleSubmit((values) => search.mutate(values.name))} noValidate>
            <Stack direction="row" spacing={1} sx={{ alignItems: "flex-start" }}>
              <TextField
                size="small"
                label={merchant ? "Switch merchant (name)" : "Merchant name"}
                placeholder="e.g. Falcon"
                error={!!errors.name}
                helperText={errors.name?.message}
                sx={{ minWidth: 240 }}
                {...register("name")}
              />
              <Button type="submit" variant="contained" startIcon={<SearchIcon />} disabled={search.isPending}>
                {search.isPending ? "Searching…" : "Search"}
              </Button>
            </Stack>
          </Box>
        </Stack>

        {errorMessage && (
          <Alert severity="warning" sx={{ mt: 2 }} onClose={() => search.reset()}>
            {errorMessage}
          </Alert>
        )}

        {search.isSuccess && (
          <Box sx={{ mt: 2 }}>
            {search.data.length === 0 ? (
              <Alert severity="info" onClose={() => search.reset()}>
                No merchants match that name.
              </Alert>
            ) : (
              <List dense disablePadding>
                {search.data.map((result) => (
                  <ListItemButton key={result.merchantId} onClick={() => select(result)}>
                    <ListItemText
                      primary={result.nameEn ?? result.nameAr ?? result.merchantId}
                      secondary={`Merchant ID: ${result.merchantId}`}
                    />
                  </ListItemButton>
                ))}
              </List>
            )}
          </Box>
        )}
      </CardContent>
    </Card>
  );
}
