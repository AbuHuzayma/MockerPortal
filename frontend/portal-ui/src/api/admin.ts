import { apiClient, type ApiEnvelope } from "./client";

export interface UserSummary {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface RoleSummary {
  name: string;
  description?: string | null;
  permissionCodes: string[];
}

export interface PermissionInfo {
  code: string;
  description: string;
  category: string;
}

export interface ScreenAdminInfo {
  code: string;
  name: string;
  description?: string | null;
  category: string;
  isActive: boolean;
  displayOrder: number;
  screenPermissions: string[];
  fields: { fieldKey: string; label: string; dataType: string; controlType: string; required: boolean; editable: boolean }[];
  actions: { code: string; label: string; requiresConfirmation: boolean }[];
}

export interface ProviderStatus {
  name: string;
  mode: string;
  baseUrl?: string | null;
}

export interface IntegrationsStatus {
  environment: string;
  providers: ProviderStatus[];
}

export async function listUsers(): Promise<UserSummary[]> {
  const response = await apiClient.get<ApiEnvelope<UserSummary[]>>("/admin/users");
  return response.data.data ?? [];
}

export async function createUser(payload: { email: string; fullName: string; password: string; role: string }): Promise<UserSummary> {
  const response = await apiClient.post<ApiEnvelope<UserSummary>>("/admin/users", payload);
  return response.data.data!;
}

export async function setUserRole(id: string, role: string): Promise<UserSummary> {
  const response = await apiClient.put<ApiEnvelope<UserSummary>>(`/admin/users/${id}/role`, { role });
  return response.data.data!;
}

export async function setUserActive(id: string, isActive: boolean): Promise<UserSummary> {
  const response = await apiClient.post<ApiEnvelope<UserSummary>>(`/admin/users/${id}/${isActive ? "enable" : "disable"}`);
  return response.data.data!;
}

export async function resetUserPassword(id: string, newPassword: string): Promise<void> {
  await apiClient.post(`/admin/users/${id}/reset-password`, { newPassword });
}

export async function listRoles(): Promise<RoleSummary[]> {
  const response = await apiClient.get<ApiEnvelope<RoleSummary[]>>("/admin/roles");
  return response.data.data ?? [];
}

export async function listPermissionCatalog(): Promise<PermissionInfo[]> {
  const response = await apiClient.get<ApiEnvelope<PermissionInfo[]>>("/admin/permissions");
  return response.data.data ?? [];
}

export async function listAdminScreens(): Promise<ScreenAdminInfo[]> {
  const response = await apiClient.get<ApiEnvelope<ScreenAdminInfo[]>>("/admin/screens");
  return response.data.data ?? [];
}

export async function getIntegrationsStatus(): Promise<IntegrationsStatus> {
  const response = await apiClient.get<ApiEnvelope<IntegrationsStatus>>("/admin/integrations");
  return response.data.data!;
}
