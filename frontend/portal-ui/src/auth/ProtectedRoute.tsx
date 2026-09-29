import { Navigate, Outlet, useLocation } from "react-router-dom";
import { Box, CircularProgress } from "@mui/material";
import { useAuth } from "./useAuth";

interface ProtectedRouteProps {
  /** When set, the route also requires this permission (UX only — the backend
   * re-checks on every request; see docs/03-security.md §2). */
  permission?: string;
}

export function ProtectedRoute({ permission }: ProtectedRouteProps) {
  const { isAuthenticated, isInitializing, hasPermission } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (permission && !hasPermission(permission)) {
    return <Navigate to="/forbidden" replace />;
  }

  return <Outlet />;
}
