import { Chip, Skeleton } from "@mui/material";
import { useEnvironment } from "../app/useEnvironment";

/**
 * Always-visible environment indicator (docs/11-ui-design.md §4) — must never
 * be easy to miss, so it renders as a solid, labeled chip, never just a color dot.
 */
export function EnvironmentBadge() {
  const { data, isLoading } = useEnvironment();

  if (isLoading || !data) {
    return <Skeleton variant="rounded" width={90} height={28} />;
  }

  return (
    <Chip
      label={data.label}
      size="medium"
      sx={{
        backgroundColor: data.color,
        color: "#FFFFFF",
        fontWeight: 700,
        letterSpacing: 0.5,
      }}
    />
  );
}
