import { Card, CardContent, Typography } from "@mui/material";

export function ForbiddenPage() {
  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Access denied
        </Typography>
        <Typography variant="body1" color="text.secondary">
          You don't have permission to view this screen. Contact an administrator if you believe this is incorrect.
        </Typography>
      </CardContent>
    </Card>
  );
}
