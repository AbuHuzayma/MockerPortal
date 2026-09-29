import type { ThemeOptions } from "@mui/material/styles";
import { colors } from "./colors";

export const components: ThemeOptions["components"] = {
  MuiCssBaseline: {
    styleOverrides: {
      body: { backgroundColor: colors.background.default },
    },
  },
  MuiPaper: {
    styleOverrides: {
      root: { borderRadius: 12 },
    },
    defaultProps: { elevation: 1 },
  },
  MuiCard: {
    styleOverrides: {
      root: {
        borderRadius: 12,
        border: `1px solid ${colors.divider}`,
      },
    },
  },
  MuiButton: {
    styleOverrides: {
      root: { borderRadius: 8, paddingInline: 16 },
    },
    defaultProps: { disableElevation: true },
  },
  MuiTableCell: {
    styleOverrides: {
      root: { paddingTop: 8, paddingBottom: 8 },
    },
  },
  MuiChip: {
    styleOverrides: {
      root: { fontWeight: 600 },
    },
  },
};
