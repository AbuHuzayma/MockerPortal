import { z } from "zod";
import type { ScreenField } from "./types";

/**
 * Builds a Zod schema from screen field metadata — client-side validation only,
 * mirroring but never replacing the server's FluentValidation check (docs/07 §4).
 */
export function buildSchemaFromFields(fields: ScreenField[]) {
  const shape: Record<string, z.ZodTypeAny> = {};

  for (const field of fields) {
    if (!field.editable) {
      shape[field.fieldKey] = z.any().optional().nullable();
      continue;
    }

    let schema: z.ZodTypeAny;
    switch (field.dataType) {
      case "Number":
      case "Decimal":
        schema = field.required
          ? z.coerce.number({ message: `${field.label} must be a number` })
          : z.coerce.number().optional().nullable();
        break;
      case "Boolean":
        schema = z.boolean().optional();
        break;
      default:
        schema = field.required
          ? z.string().min(1, `${field.label} is required`)
          : z.string().optional().nullable();
    }

    shape[field.fieldKey] = schema;
  }

  return z.object(shape);
}
