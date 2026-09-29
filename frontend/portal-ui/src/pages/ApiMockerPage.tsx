import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  Typography,
  Button,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Chip,
  CircularProgress,
  Alert,
  Stack,
  IconButton,
  Tooltip,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/EditOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import PowerSettingsNewIcon from "@mui/icons-material/PowerSettingsNew";
import { useAuth } from "../auth/useAuth";
import {
  listMockApis,
  upsertMockApi,
  deleteMockApi,
  enableMockApi,
  disableMockApi,
  emptyMockApi,
  type MockApiDto,
} from "../api/mockAdmin";
import { MockApiEditorDialog } from "../components/api-mocker/MockApiEditorDialog";

interface ApiMockerPageProps {
  title: string;
  /** Exact codes shown on this page's list, or "other" to show every code not covered by another page. */
  codes: string[] | "other";
  defaultCodePrefix: string;
}

const KNOWN_CODES = ["ABSHER", "YAKEEN", "ELM"];

export function ApiMockerPage({ title, codes, defaultCodePrefix }: ApiMockerPageProps) {
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();
  const [editorState, setEditorState] = useState<{ dto: MockApiDto; isNew: boolean } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const canManage = hasPermission("api-mocker.manage");
  const canEnable = hasPermission("api-mocker.enable");
  const canDisable = hasPermission("api-mocker.disable");

  const apisQuery = useQuery({ queryKey: ["mock-apis"], queryFn: listMockApis });

  const visibleApis = (apisQuery.data ?? []).filter((api) =>
    codes === "other" ? !KNOWN_CODES.includes(api.code) : codes.includes(api.code),
  );

  const saveMutation = useMutation({
    mutationFn: (dto: MockApiDto) => upsertMockApi(dto.code, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mock-apis"] });
      setEditorState(null);
      setError(null);
    },
    onError: (err: unknown) => setError(extractErrorMessage(err, "Failed to save the mock API.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (code: string) => deleteMockApi(code),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mock-apis"] });
      setError(null);
    },
    onError: (err: unknown) => setError(extractErrorMessage(err, "Failed to delete the mock API.")),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ code, isActive }: { code: string; isActive: boolean }) =>
      isActive ? disableMockApi(code) : enableMockApi(code),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mock-apis"] });
      setError(null);
    },
    onError: (err: unknown) => setError(extractErrorMessage(err, "Failed to change the mock API's status.")),
  });

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", mb: 2 }}>
          <div>
            <Typography variant="h3" gutterBottom>
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Configure canned responses served from <code>/mock/&#123;code&#125;/...</code> — see docs/09-api-mocker.md.
            </Typography>
          </div>
          {canManage && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() =>
                setEditorState({
                  dto: emptyMockApi(codes === "other" ? "" : defaultCodePrefix, "DEV"),
                  isNew: true,
                })
              }
            >
              New Mock API
            </Button>
          )}
        </Stack>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {apisQuery.isLoading && <CircularProgress size={28} />}
        {apisQuery.isError && <Alert severity="error">Unable to load API Mocker configuration.</Alert>}

        {apisQuery.data && visibleApis.length === 0 && (
          <Typography variant="body2" color="text.secondary">
            No mock APIs configured here yet.
          </Typography>
        )}

        {visibleApis.length > 0 && (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Environment</TableCell>
                <TableCell>Endpoints</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {visibleApis.map((api) => (
                <TableRow key={`${api.code}-${api.environment}`}>
                  <TableCell>{api.code}</TableCell>
                  <TableCell>{api.name}</TableCell>
                  <TableCell>{api.environment}</TableCell>
                  <TableCell>{api.endpoints.length}</TableCell>
                  <TableCell>
                    <Chip
                      label={api.isActive ? "Enabled" : "Disabled"}
                      color={api.isActive ? "success" : "default"}
                      size="small"
                    />
                  </TableCell>
                  <TableCell align="right">
                    {((api.isActive && canDisable) || (!api.isActive && canEnable)) && (
                      <Tooltip title={api.isActive ? "Disable" : "Enable"}>
                        <IconButton
                          size="small"
                          onClick={() => toggleMutation.mutate({ code: api.code, isActive: api.isActive })}
                          disabled={toggleMutation.isPending}
                        >
                          <PowerSettingsNewIcon fontSize="small" color={api.isActive ? "success" : "disabled"} />
                        </IconButton>
                      </Tooltip>
                    )}
                    {canManage && (
                      <Tooltip title="Edit">
                        <IconButton size="small" onClick={() => setEditorState({ dto: api, isNew: false })}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    {canManage && (
                      <Tooltip title="Delete">
                        <IconButton
                          size="small"
                          onClick={() => {
                            if (window.confirm(`Delete mock API "${api.code}"? This cannot be undone.`)) {
                              deleteMutation.mutate(api.code);
                            }
                          }}
                          disabled={deleteMutation.isPending}
                        >
                          <DeleteOutlineIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      {editorState && (
        <MockApiEditorDialog
          open
          initial={editorState.dto}
          isNew={editorState.isNew}
          isSaving={saveMutation.isPending}
          onClose={() => setEditorState(null)}
          onSave={(dto) => saveMutation.mutate(dto)}
        />
      )}
    </Card>
  );
}

function extractErrorMessage(err: unknown, fallback: string): string {
  const message = (err as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message;
  return message ?? fallback;
}
