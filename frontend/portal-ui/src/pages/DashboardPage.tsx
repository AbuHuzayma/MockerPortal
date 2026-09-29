import { Card, CardContent, Typography, Stack, Button } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useEnvironment } from "../app/useEnvironment";

export function DashboardPage() {
  const { data: environment } = useEnvironment();

  return (
    <Stack spacing={2}>
      <Card>
        <CardContent>
          <Typography variant="h3" gutterBottom>
            Welcome to the Test Data Management Portal
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Currently connected to the{" "}
            <strong>{environment?.label ?? "..."}</strong> environment. Use the
            navigation on the left to search customers/merchants, update test
            data, or configure the API Mocker.
          </Typography>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Typography variant="h4" gutterBottom>
            Status
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Phase 3 — Dynamic Screen Framework. Real customer/merchant screens,
            the API Mocker, and admin tooling are implemented in the phases
            that follow. See <code>docs/15-development-roadmap.md</code> in
            the repository.
          </Typography>
          <Button component={RouterLink} to="/dev/sample-screen" variant="outlined" size="small">
            View Dynamic Form Sample
          </Button>
        </CardContent>
      </Card>
    </Stack>
  );
}
