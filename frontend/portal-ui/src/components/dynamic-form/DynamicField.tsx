import { Controller, type Control, type FieldErrors } from "react-hook-form";
import {
  TextField,
  Checkbox,
  FormControlLabel,
  FormControl,
  FormLabel,
  RadioGroup,
  Radio,
  Select,
  MenuItem,
  Chip,
  Box,
  Typography,
} from "@mui/material";
import type { ScreenField, FormValues } from "./types";

interface DynamicFieldProps {
  field: ScreenField;
  control: Control<FormValues>;
  errors: FieldErrors<FormValues>;
}

export function DynamicField({ field, control, errors }: DynamicFieldProps) {
  const errorMessage = errors[field.fieldKey]?.message as string | undefined;
  const disabled = !field.editable;

  if (field.controlType === "ReadOnly") {
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: rhf }) => (
          <Box>
            <Typography variant="body2" color="text.secondary">
              {field.label}
            </Typography>
            <Typography variant="body1">{rhf.value?.toString().trim() ? String(rhf.value) : "—"}</Typography>
          </Box>
        )}
      />
    );
  }

  if (field.controlType === "Checkbox" || field.controlType === "Boolean") {
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: rhf }) => (
          <FormControlLabel
            disabled={disabled}
            control={<Checkbox checked={!!rhf.value} onChange={(e) => rhf.onChange(e.target.checked)} />}
            label={field.label}
          />
        )}
      />
    );
  }

  if (field.controlType === "Radio") {
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: rhf }) => (
          <FormControl disabled={disabled} error={!!errorMessage}>
            <FormLabel>{field.label}</FormLabel>
            <RadioGroup row value={rhf.value ?? ""} onChange={(e) => rhf.onChange(e.target.value)}>
              {field.options?.map((option) => (
                <FormControlLabel key={option.value} value={option.value} control={<Radio />} label={option.label} />
              ))}
            </RadioGroup>
          </FormControl>
        )}
      />
    );
  }

  if (field.controlType === "Select" || field.controlType === "MultiSelect") {
    const multiple = field.controlType === "MultiSelect";
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: rhf }) => (
          <FormControl fullWidth disabled={disabled} error={!!errorMessage}>
            <FormLabel sx={{ mb: 0.5, fontSize: "0.8rem" }}>{field.label}</FormLabel>
            <Select
              multiple={multiple}
              value={multiple ? (rhf.value ?? []) : (rhf.value ?? "")}
              onChange={(e) => rhf.onChange(e.target.value)}
              renderValue={
                multiple
                  ? (selected) => (
                      <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap" }}>
                        {(selected as string[]).map((value) => (
                          <Chip key={value} label={field.options?.find((o) => o.value === value)?.label ?? value} size="small" />
                        ))}
                      </Box>
                    )
                  : undefined
              }
            >
              {field.options?.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}
      />
    );
  }

  if (field.controlType === "TextArea") {
    return (
      <Controller
        name={field.fieldKey}
        control={control}
        render={({ field: rhf }) => (
          <TextField
            {...rhf}
            value={rhf.value ?? ""}
            label={field.label}
            multiline
            minRows={3}
            fullWidth
            disabled={disabled}
            error={!!errorMessage}
            helperText={errorMessage}
          />
        )}
      />
    );
  }

  const inputTypeByControl: Partial<Record<ScreenField["controlType"], string>> = {
    Number: "number",
    Decimal: "number",
    Date: "date",
    DateTime: "datetime-local",
  };
  const inputType = inputTypeByControl[field.controlType] ?? "text";

  return (
    <Controller
      name={field.fieldKey}
      control={control}
      render={({ field: rhf }) => (
        <TextField
          {...rhf}
          value={rhf.value ?? ""}
          type={inputType}
          label={field.label}
          fullWidth
          disabled={disabled}
          required={field.required}
          error={!!errorMessage}
          helperText={errorMessage}
          slotProps={{ inputLabel: inputType === "date" || inputType === "datetime-local" ? { shrink: true } : undefined }}
        />
      )}
    />
  );
}
