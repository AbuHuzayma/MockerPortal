import { useQuery } from "@tanstack/react-query";
import { Accordion, AccordionSummary, AccordionDetails, Typography, Chip, Stack, CircularProgress, Alert, Table, TableHead, TableRow, TableCell, TableBody } from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { listAdminScreens } from "../../api/admin";

export function ScreensTab() {
  const screensQuery = useQuery({ queryKey: ["admin-screens"], queryFn: listAdminScreens });

  if (screensQuery.isLoading) {
    return <CircularProgress size={28} />;
  }
  if (screensQuery.isError) {
    return <Alert severity="error">Unable to load screens.</Alert>;
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="body2" color="text.secondary">
        Screens are seeded from code (ScreenSeeder), not admin-editable — this is a read-only view for review/audit purposes.
      </Typography>
      {(screensQuery.data ?? []).map((screen) => (
        <Accordion key={screen.code}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexGrow: 1 }}>
              <Typography sx={{ flexGrow: 1 }}>
                {screen.name} ({screen.code}) — {screen.category}
              </Typography>
              <Chip label={screen.isActive ? "Active" : "Inactive"} size="small" color={screen.isActive ? "success" : "default"} />
            </Stack>
          </AccordionSummary>
          <AccordionDetails>
            {screen.screenPermissions.length > 0 && (
              <Typography variant="body2" sx={{ mb: 1.5 }}>
                Required to view: {screen.screenPermissions.join(", ")}
              </Typography>
            )}

            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              Fields ({screen.fields.length})
            </Typography>
            <Table size="small" sx={{ mb: 2 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Key</TableCell>
                  <TableCell>Label</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Control</TableCell>
                  <TableCell>Required</TableCell>
                  <TableCell>Editable</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {screen.fields.map((f) => (
                  <TableRow key={f.fieldKey}>
                    <TableCell>{f.fieldKey}</TableCell>
                    <TableCell>{f.label}</TableCell>
                    <TableCell>{f.dataType}</TableCell>
                    <TableCell>{f.controlType}</TableCell>
                    <TableCell>{f.required ? "Yes" : "No"}</TableCell>
                    <TableCell>{f.editable ? "Yes" : "No"}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>

            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              Actions ({screen.actions.length})
            </Typography>
            <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
              {screen.actions.map((a) => (
                <Chip key={a.code} label={`${a.label}${a.requiresConfirmation ? " (confirm)" : ""}`} size="small" />
              ))}
            </Stack>
          </AccordionDetails>
        </Accordion>
      ))}
    </Stack>
  );
}
