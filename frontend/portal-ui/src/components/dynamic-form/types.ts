export interface ScreenFieldOption {
  value: string;
  label: string;
}

export interface ScreenField {
  fieldKey: string;
  label: string;
  dataType: "Text" | "Number" | "Decimal" | "Date" | "DateTime" | "Boolean";
  controlType:
    | "Text"
    | "Number"
    | "Decimal"
    | "Date"
    | "DateTime"
    | "Boolean"
    | "Checkbox"
    | "Radio"
    | "Select"
    | "MultiSelect"
    | "TextArea"
    | "ReadOnly";
  required: boolean;
  editable: boolean;
  displayOrder: number;
  integrationKey: string;
  options: ScreenFieldOption[] | null;
}

export interface ScreenAction {
  code: string;
  label: string;
  requiresConfirmation: boolean;
}

export interface ScreenDefinition {
  code: string;
  name: string;
  description: string | null;
  fields: ScreenField[];
  actions: ScreenAction[];
}

export type FormValues = Record<string, unknown>;
