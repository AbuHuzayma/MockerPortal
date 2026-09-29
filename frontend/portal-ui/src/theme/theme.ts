import { createTheme } from "@mui/material/styles";
import { colors } from "./colors";
import { typography } from "./typography";
import { components } from "./components";

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: colors.primary,
    background: colors.background,
    text: colors.text,
    success: colors.success,
    warning: colors.warning,
    error: colors.error,
    info: colors.info,
    divider: colors.divider,
  },
  shape: { borderRadius: 12 },
  typography,
  components,
});
