import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Chip,
  Button,
  IconButton,
  Tooltip,
  CircularProgress,
  Alert,
  Stack,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import LockResetIcon from "@mui/icons-material/LockResetOutlined";
import PowerSettingsNewIcon from "@mui/icons-material/PowerSettingsNew";
import {
  listUsers,
  createUser,
  setUserRole,
  setUserActive,
  resetUserPassword,
  listRoles,
  type UserSummary,
} from "../../api/admin";
import { useAuth } from "../../auth/useAuth";

export function UsersTab() {
  const { user: currentUser } = useAuth();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [resetTarget, setResetTarget] = useState<UserSummary | null>(null);

  const usersQuery = useQuery({ queryKey: ["admin-users"], queryFn: listUsers });
  const rolesQuery = useQuery({ queryKey: ["admin-roles"], queryFn: listRoles });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["admin-users"] });
  const handleError = (err: unknown, fallback: string) =>
    setError((err as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message ?? fallback);

  const roleMutation = useMutation({
    mutationFn: ({ id, role }: { id: string; role: string }) => setUserRole(id, role),
    onSuccess: invalidate,
    onError: (err) => handleError(err, "Failed to change the user's role."),
  });

  const activeMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => setUserActive(id, isActive),
    onSuccess: invalidate,
    onError: (err) => handleError(err, "Failed to change the user's status."),
  });

  return (
    <Stack spacing={2}>
      {error && (
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          New User
        </Button>
      </Stack>

      {usersQuery.isLoading && <CircularProgress size={28} />}
      {usersQuery.isError && <Alert severity="error">Unable to load users.</Alert>}

      {usersQuery.data && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Email</TableCell>
              <TableCell>Full name</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {usersQuery.data.map((u) => (
              <TableRow key={u.id}>
                <TableCell>{u.email}</TableCell>
                <TableCell>{u.fullName}</TableCell>
                <TableCell>
                  <TextField
                    select
                    size="small"
                    value={u.roles[0] ?? ""}
                    onChange={(e) => roleMutation.mutate({ id: u.id, role: e.target.value })}
                    disabled={roleMutation.isPending}
                    sx={{ minWidth: 140 }}
                  >
                    {(rolesQuery.data ?? []).map((r) => (
                      <MenuItem key={r.name} value={r.name}>
                        {r.name}
                      </MenuItem>
                    ))}
                  </TextField>
                </TableCell>
                <TableCell>
                  <Chip label={u.isActive ? "Active" : "Disabled"} color={u.isActive ? "success" : "default"} size="small" />
                </TableCell>
                <TableCell align="right">
                  <Tooltip title={u.isActive ? "Disable" : "Enable"}>
                    <span>
                      <IconButton
                        size="small"
                        disabled={activeMutation.isPending || u.id === currentUser?.id}
                        onClick={() => activeMutation.mutate({ id: u.id, isActive: !u.isActive })}
                      >
                        <PowerSettingsNewIcon fontSize="small" color={u.isActive ? "success" : "disabled"} />
                      </IconButton>
                    </span>
                  </Tooltip>
                  <Tooltip title="Reset password">
                    <IconButton size="small" onClick={() => setResetTarget(u)}>
                      <LockResetIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {createOpen && (
        <CreateUserDialog
          roles={(rolesQuery.data ?? []).map((r) => r.name)}
          onClose={() => setCreateOpen(false)}
          onCreated={() => {
            setCreateOpen(false);
            invalidate();
          }}
          onError={(msg) => setError(msg)}
        />
      )}

      {resetTarget && (
        <ResetPasswordDialog user={resetTarget} onClose={() => setResetTarget(null)} onError={(msg) => setError(msg)} />
      )}
    </Stack>
  );
}

function CreateUserDialog({
  roles,
  onClose,
  onCreated,
  onError,
}: {
  roles: string[];
  onClose: () => void;
  onCreated: () => void;
  onError: (message: string) => void;
}) {
  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState(roles[0] ?? "");

  const mutation = useMutation({
    mutationFn: () => createUser({ email, fullName, password, role }),
    onSuccess: onCreated,
    onError: (err: unknown) =>
      onError((err as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message ?? "Failed to create the user."),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>New User</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField label="Email" value={email} onChange={(e) => setEmail(e.target.value)} fullWidth />
          <TextField label="Full name" value={fullName} onChange={(e) => setFullName(e.target.value)} fullWidth />
          <TextField
            label="Initial password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            fullWidth
            helperText="Min 12 chars, upper/lower/digit/symbol — docs/04 §1"
          />
          <TextField select label="Role" value={role} onChange={(e) => setRole(e.target.value)} fullWidth>
            {roles.map((r) => (
              <MenuItem key={r} value={r}>
                {r}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || !email || !fullName || !password || !role}
        >
          Create
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ResetPasswordDialog({ user, onClose, onError }: { user: UserSummary; onClose: () => void; onError: (message: string) => void }) {
  const [password, setPassword] = useState("");
  const [done, setDone] = useState(false);

  const mutation = useMutation({
    mutationFn: () => resetUserPassword(user.id, password),
    onSuccess: () => setDone(true),
    onError: (err: unknown) =>
      onError((err as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message ?? "Failed to reset the password."),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Reset password — {user.email}</DialogTitle>
      <DialogContent>
        {done ? (
          <Alert severity="success" sx={{ mt: 1 }}>
            Password reset.
          </Alert>
        ) : (
          <TextField
            label="New password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            fullWidth
            sx={{ mt: 1 }}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{done ? "Close" : "Cancel"}</Button>
        {!done && (
          <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending || !password}>
            Reset
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
