/**
 * Placeholder STC Bank-inspired palette. Swap these values (only) when official
 * brand assets/hex codes are supplied — see docs/11-ui-design.md §3.
 * No component should hardcode a color outside this file.
 */
export const colors = {
  primary: {
    main: "#5B2A86",
    dark: "#3E1C5E",
    light: "#8455AD",
    contrastText: "#FFFFFF",
  },
  background: {
    default: "#F5F4F8",
    paper: "#FFFFFF",
  },
  text: {
    primary: "#1E1B22",
    secondary: "#6B6572",
  },
  success: { main: "#2E7D32" },
  warning: { main: "#ED6C02" },
  error: { main: "#D32F2F" },
  info: { main: "#0288D1" },
  divider: "#E4E1EA",
} as const;

/** Environment badge colors — intentionally distinct from the brand palette. */
export const environmentColors: Record<string, string> = {
  DEV: "#6B6572",
  QA: "#0288D1",
  PREPROD: "#ED6C02",
};
