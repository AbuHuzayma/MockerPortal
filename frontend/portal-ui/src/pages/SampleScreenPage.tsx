import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, Typography, Chip, CircularProgress, Alert, Snackbar } from "@mui/material";
import { apiClient, type ApiEnvelope } from "../api/client";
import { DynamicForm } from "../components/dynamic-form/DynamicForm";
import type { FormValues } from "../components/dynamic-form/types";
import { ScreenCodes } from "../components/dynamic-form/screenCodes";

export function SampleScreenPage() {
  const queryClient = useQueryClient();
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const recordQuery = useQuery({
    queryKey: ["sample-record"],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>("/sample-screen/record");
      return response.data.data!;
    },
  });

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const response = await apiClient.post<ApiEnvelope<FormValues>>("/sample-screen/save", values);
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["sample-record"], data);
      setSaveError(null);
      setSaveSuccess(true);
    },
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: ApiEnvelope<unknown> } })?.response?.data?.error?.message ??
        "Save failed. Please try again.";
      setSaveError(message);
    },
  });

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Dynamic Form Sample
          <Chip label="Phase 3 proof-of-concept" size="small" sx={{ ml: 1.5 }} />
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Exercises every supported control type end-to-end (metadata API → DynamicForm →
          save → audit). Not a real business screen — see docs/07-dynamic-screen-engine.md §7.
        </Typography>

        {recordQuery.isLoading && <CircularProgress size={28} />}
        {recordQuery.isError && <Alert severity="error">Unable to load the sample record.</Alert>}

        {saveError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setSaveError(null)}>
            {saveError}
          </Alert>
        )}

        {recordQuery.data && (
          <DynamicForm
            screenCode={ScreenCodes.SampleScreen}
            values={recordQuery.data}
            onSubmit={async (_actionCode, values) => {
              await saveMutation.mutateAsync(values);
            }}
            isSaving={saveMutation.isPending}
          />
        )}
      </CardContent>

      <Snackbar
        open={saveSuccess}
        autoHideDuration={3000}
        onClose={() => setSaveSuccess(false)}
        message="Saved successfully"
      />
    </Card>
  );
}
