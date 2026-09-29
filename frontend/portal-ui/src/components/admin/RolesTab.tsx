import { useQuery } from "@tanstack/react-query";
import { Accordion, AccordionSummary, AccordionDetails, Typography, Chip, Stack, CircularProgress, Alert } from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { listRoles, listPermissionCatalog } from "../../api/admin";

export function RolesTab() {
  const rolesQuery = useQuery({ queryKey: ["admin-roles"], queryFn: listRoles });
  const permissionsQuery = useQuery({ queryKey: ["admin-permissions"], queryFn: listPermissionCatalog });

  if (rolesQuery.isLoading || permissionsQuery.isLoading) {
    return <CircularProgress size={28} />;
  }
  if (rolesQuery.isError || permissionsQuery.isError) {
    return <Alert severity="error">Unable to load roles/permissions.</Alert>;
  }

  const descriptionByCode = new Map((permissionsQuery.data ?? []).map((p) => [p.code, p.description]));

  return (
    <Stack spacing={1.5}>
      <Typography variant="body2" color="text.secondary">
        Roles and the permission catalog are defined in code (reviewed changes), not editable here — see docs/07 §6.
      </Typography>
      {(rolesQuery.data ?? []).map((role) => (
        <Accordion key={role.name}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography sx={{ flexGrow: 1 }}>
              {role.name} — {role.permissionCodes.length} permission(s)
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            {role.description && (
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                {role.description}
              </Typography>
            )}
            <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
              {role.permissionCodes.map((code) => (
                <Chip key={code} label={code} size="small" title={descriptionByCode.get(code) ?? ""} />
              ))}
            </Stack>
          </AccordionDetails>
        </Accordion>
      ))}
    </Stack>
  );
}
