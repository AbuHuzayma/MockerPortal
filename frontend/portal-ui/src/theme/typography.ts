import type { ThemeOptions } from "@mui/material/styles";

export const typography: ThemeOptions["typography"] = {
  fontFamily: [
    "Inter",
    "-apple-system",
    "BlinkMacSystemFont",
    '"Segoe UI"',
    "Roboto",
    "Helvetica",
    "Arial",
    "sans-serif",
  ].join(","),
  h1: { fontSize: "2rem", fontWeight: 600 },
  h2: { fontSize: "1.5rem", fontWeight: 600 },
  h3: { fontSize: "1.25rem", fontWeight: 600 },
  h4: { fontSize: "1.1rem", fontWeight: 600 },
  subtitle1: { fontSize: "0.95rem", fontWeight: 500 },
  body1: { fontSize: "0.9rem" },
  body2: { fontSize: "0.825rem" },
  button: { textTransform: "none", fontWeight: 600 },
};
