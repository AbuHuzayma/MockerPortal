import { useState } from "react";
import { Card, CardContent, Typography, Tabs, Tab, Box } from "@mui/material";
import { useAuth } from "../auth/useAuth";
import { UsersTab } from "../components/admin/UsersTab";
import { RolesTab } from "../components/admin/RolesTab";
import { ScreensTab } from "../components/admin/ScreensTab";
import { IntegrationsTab } from "../components/admin/IntegrationsTab";

const ALL_TABS = [
  { key: "users", label: "Users", permission: "admin.users", render: () => <UsersTab /> },
  { key: "roles", label: "Roles & Permissions", permission: "admin.roles", render: () => <RolesTab /> },
  { key: "screens", label: "Screens", permission: "admin.screens", render: () => <ScreensTab /> },
  { key: "integrations", label: "Integrations", permission: "admin.integrations", render: () => <IntegrationsTab /> },
];

export function AdminPage() {
  const { hasPermission } = useAuth();
  const visibleTabs = ALL_TABS.filter((t) => hasPermission(t.permission));
  const [active, setActive] = useState(visibleTabs[0]?.key ?? "");

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Administration
        </Typography>

        {visibleTabs.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            You do not have access to any administration section.
          </Typography>
        ) : (
          <>
            <Tabs value={active} onChange={(_, value) => setActive(value)} sx={{ mb: 2, borderBottom: 1, borderColor: "divider" }}>
              {visibleTabs.map((t) => (
                <Tab key={t.key} value={t.key} label={t.label} />
              ))}
            </Tabs>
            <Box>{visibleTabs.find((t) => t.key === active)?.render()}</Box>
          </>
        )}
      </CardContent>
    </Card>
  );
}
