import { useQuery } from "@tanstack/react-query";
import { Table, TableHead, TableRow, TableCell, TableBody, Chip, Typography, Stack, CircularProgress, Alert } from "@mui/material";
import { getIntegrationsStatus } from "../../api/admin";

const MODE_COLOR: Record<string, "default" | "success" | "warning"> = {
  Mock: "default",
  Sql: "warning",
  Real: "success",
};

export function IntegrationsTab() {
  const statusQuery = useQuery({ queryKey: ["admin-integrations"], queryFn: getIntegrationsStatus });

  if (statusQuery.isLoading) {
    return <CircularProgress size={28} />;
  }
  if (statusQuery.isError) {
    return <Alert severity="error">Unable to load integration status.</Alert>;
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="body2" color="text.secondary">
        Mode is set via configuration per environment (currently <strong>{statusQuery.data!.environment}</strong>) — not
        runtime-editable here; changing it is a deployment action, reviewed like any other config change (docs/12 §2).
      </Typography>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Provider</TableCell>
            <TableCell>Mode</TableCell>
            <TableCell>Base URL</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {statusQuery.data!.providers.map((p) => (
            <TableRow key={p.name}>
              <TableCell>{p.name}</TableCell>
              <TableCell>
                <Chip label={p.mode} size="small" color={MODE_COLOR[p.mode] ?? "default"} />
              </TableCell>
              <TableCell>{p.baseUrl ?? "—"}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Stack>
  );
}
