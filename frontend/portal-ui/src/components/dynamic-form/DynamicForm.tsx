import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import {
  Grid,
  Box,
  Button,
  Stack,
  CircularProgress,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Table,
  TableBody,
  TableRow,
  TableCell,
  Typography,
} from "@mui/material";
import { apiClient, type ApiEnvelope } from "../../api/client";
import { buildSchemaFromFields } from "./buildSchema";
import { DynamicField } from "./DynamicField";
import type { ScreenAction, ScreenDefinition, FormValues } from "./types";

interface DynamicFormProps {
  screenCode: string;
  values: FormValues;
  onSubmit: (actionCode: string, values: FormValues) => Promise<void>;
  isSaving?: boolean;
}

export function DynamicForm({ screenCode, values, onSubmit, isSaving }: DynamicFormProps) {
  const screenQuery = useQuery({
    queryKey: ["screen-definition", screenCode],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<ScreenDefinition>>(`/screens/${screenCode}`);
      return response.data.data!;
    },
    staleTime: 5 * 60 * 1000,
  });

  const fields = useMemo(() => screenQuery.data?.fields ?? [], [screenQuery.data]);
  const schema = useMemo(() => buildSchemaFromFields(fields), [fields]);

  const {
    control,
    handleSubmit,
    reset,
    getValues,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: values });

  useEffect(() => {
    reset(values);
  }, [values, reset]);

  const [pendingAction, setPendingAction] = useState<ScreenAction | null>(null);
  const [pendingValues, setPendingValues] = useState<FormValues | null>(null);
  // Snapshotted at dialog-open time — reading the live `values` prop instead
  // caused a one-frame "no fields changed" flash, because the parent updates
  // its query cache (and so `values`) before this component clears
  // pendingAction, briefly making before === after.
  const [pendingBeforeValues, setPendingBeforeValues] = useState<FormValues | null>(null);

  if (screenQuery.isLoading) {
    return <CircularProgress size={28} />;
  }

  if (screenQuery.isError || !screenQuery.data) {
    return <Alert severity="error">Unable to load this screen's definition.</Alert>;
  }

  const requestAction = (action: ScreenAction) => {
    if (action.requiresConfirmation) {
      setPendingAction(action);
      setPendingValues(getValues());
      setPendingBeforeValues(values);
    } else {
      handleSubmit((formValues) => onSubmit(action.code, formValues))();
    }
  };

  const confirmAndSubmit = async () => {
    if (!pendingAction || !pendingValues) return;
    await onSubmit(pendingAction.code, pendingValues);
    setPendingAction(null);
    setPendingValues(null);
    setPendingBeforeValues(null);
  };

  const changedFields =
    pendingValues &&
    pendingBeforeValues &&
    fields.filter((f) => f.editable && String(pendingBeforeValues[f.fieldKey] ?? "") !== String(pendingValues[f.fieldKey] ?? ""));

  return (
    <Box component="form" noValidate>
      <Grid container spacing={2.5}>
        {fields.map((field) => (
          <Grid key={field.fieldKey} size={{ xs: 12, sm: 6, md: 4 }}>
            <DynamicField field={field} control={control} errors={errors} />
          </Grid>
        ))}
      </Grid>

      <Stack direction="row" spacing={1.5} sx={{ mt: 3 }}>
        {screenQuery.data.actions.map((action) => (
          <Button key={action.code} variant="contained" disabled={isSaving} onClick={() => requestAction(action)}>
            {isSaving ? "Saving…" : action.label}
          </Button>
        ))}
      </Stack>

      <Dialog open={!!pendingAction} onClose={() => setPendingAction(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Confirm: {pendingAction?.label}</DialogTitle>
        <DialogContent>
          {changedFields && changedFields.length > 0 ? (
            <Table size="small">
              <TableBody>
                {changedFields.map((f) => (
                  <TableRow key={f.fieldKey}>
                    <TableCell sx={{ fontWeight: 600 }}>{f.label}</TableCell>
                    <TableCell>{String(pendingBeforeValues?.[f.fieldKey] ?? "—")}</TableCell>
                    <TableCell>→</TableCell>
                    <TableCell>{String(pendingValues?.[f.fieldKey] ?? "—")}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <Typography variant="body2" color="text.secondary">
              No fields changed.
            </Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPendingAction(null)}>Cancel</Button>
          <Button variant="contained" onClick={confirmAndSubmit} disabled={isSaving}>
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
