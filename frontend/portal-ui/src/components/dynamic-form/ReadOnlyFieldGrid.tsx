import { Grid, Typography } from "@mui/material";
import type { FormValues, ScreenField } from "./types";

function formatValue(field: ScreenField, value: unknown): string {
  if (value === null || value === undefined || value === "") {
    return "—";
  }
  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }
  const labelFor = (v: unknown) => field.options?.find((o) => o.value === String(v))?.label ?? String(v);
  if (Array.isArray(value)) {
    return value.length > 0 ? value.map(labelFor).join(", ") : "—";
  }
  return labelFor(value);
}

/** Renders a screen's metadata-defined fields as label/value pairs, with no inputs or actions. */
export function ReadOnlyFieldGrid({ fields, values }: { fields: ScreenField[]; values: FormValues }) {
  return (
    <Grid container spacing={2}>
      {fields.map((field) => (
        <Grid key={field.fieldKey} size={{ xs: 12, sm: 6, md: 4 }}>
          <Typography variant="body2" color="text.secondary">
            {field.label}
          </Typography>
          <Typography variant="body1" sx={{ overflowWrap: "anywhere" }}>
            {formatValue(field, values[field.fieldKey])}
          </Typography>
        </Grid>
      ))}
    </Grid>
  );
}
