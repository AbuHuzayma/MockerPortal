import { apiClient, type ApiEnvelope } from "./client";

export interface AuditLogEntry {
  id: string;
  username: string;
  environment: string;
  customerId?: string | null;
  merchantId?: string | null;
  screen: string;
  operation: string;
  entity: string;
  field?: string | null;
  oldValue?: string | null;
  newValue?: string | null;
  result: string;
  errorMessage?: string | null;
  correlationId: string;
  timestamp: string;
}

export interface AuditLogPage {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuditLogFilters {
  fromUtc?: string;
  toUtc?: string;
  username?: string;
  customerId?: string;
  merchantId?: string;
  screen?: string;
  entity?: string;
  result?: string;
  page?: number;
  pageSize?: number;
}

export async function queryAuditLog(filters: AuditLogFilters): Promise<AuditLogPage> {
  const params = Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== undefined && v !== ""));
  const response = await apiClient.get<ApiEnvelope<AuditLogPage>>("/audit", { params });
  return response.data.data ?? { items: [], totalCount: 0, page: 1, pageSize: 50 };
}
