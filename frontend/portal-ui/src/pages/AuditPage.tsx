import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  Typography,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  TextField,
  Stack,
  Button,
  CircularProgress,
  Alert,
  Chip,
  Pagination,
} from "@mui/material";
import { queryAuditLog, type AuditLogFilters } from "../api/audit";

const PAGE_SIZE = 25;

export function AuditPage() {
  const [filters, setFilters] = useState<AuditLogFilters>({});
  const [draft, setDraft] = useState<AuditLogFilters>({});
  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: ["audit-log", filters, page],
    queryFn: () => queryAuditLog({ ...filters, page, pageSize: PAGE_SIZE }),
  });

  const pageCount = query.data ? Math.max(1, Math.ceil(query.data.totalCount / PAGE_SIZE)) : 1;

  return (
    <Card>
      <CardContent>
        <Typography variant="h3" gutterBottom>
          Audit Log
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Every write to a customer/merchant test-data field, plus admin and API Mocker changes — see docs/10-audit.md.
        </Typography>

        <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: "wrap", gap: 2 }}>
          <TextField
            label="Username contains"
            size="small"
            value={draft.username ?? ""}
            onChange={(e) => setDraft({ ...draft, username: e.target.value })}
          />
          <TextField
            label="Customer ID"
            size="small"
            value={draft.customerId ?? ""}
            onChange={(e) => setDraft({ ...draft, customerId: e.target.value })}
          />
          <TextField
            label="Merchant ID"
            size="small"
            value={draft.merchantId ?? ""}
            onChange={(e) => setDraft({ ...draft, merchantId: e.target.value })}
          />
          <TextField
            label="Screen"
            size="small"
            value={draft.screen ?? ""}
            onChange={(e) => setDraft({ ...draft, screen: e.target.value })}
          />
          <TextField
            label="Entity"
            size="small"
            value={draft.entity ?? ""}
            onChange={(e) => setDraft({ ...draft, entity: e.target.value })}
          />
          <Button
            variant="contained"
            onClick={() => {
              setFilters(draft);
              setPage(1);
            }}
          >
            Apply filters
          </Button>
          <Button
            onClick={() => {
              setDraft({});
              setFilters({});
              setPage(1);
            }}
          >
            Clear
          </Button>
        </Stack>

        {query.isLoading && <CircularProgress size={28} />}
        {query.isError && <Alert severity="error">Unable to load the audit log.</Alert>}

        {query.data && (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {query.data.totalCount} entries
            </Typography>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Timestamp (UTC)</TableCell>
                  <TableCell>User</TableCell>
                  <TableCell>Env</TableCell>
                  <TableCell>Screen</TableCell>
                  <TableCell>Operation</TableCell>
                  <TableCell>Entity / Field</TableCell>
                  <TableCell>Old → New</TableCell>
                  <TableCell>Result</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {query.data.items.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell>{new Date(entry.timestamp).toLocaleString()}</TableCell>
                    <TableCell>{entry.username}</TableCell>
                    <TableCell>{entry.environment}</TableCell>
                    <TableCell>{entry.screen}</TableCell>
                    <TableCell>{entry.operation}</TableCell>
                    <TableCell>
                      {entry.entity}
                      {entry.field ? `.${entry.field}` : ""}
                    </TableCell>
                    <TableCell>
                      {entry.oldValue ?? "—"} → {entry.newValue ?? "—"}
                    </TableCell>
                    <TableCell>
                      <Chip label={entry.result} size="small" color={entry.result === "Success" ? "success" : "error"} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <Stack sx={{ mt: 2, alignItems: "center" }}>
              <Pagination count={pageCount} page={page} onChange={(_, value) => setPage(value)} />
            </Stack>
          </>
        )}
      </CardContent>
    </Card>
  );
}
