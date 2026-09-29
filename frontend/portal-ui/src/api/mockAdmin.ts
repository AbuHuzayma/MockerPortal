import { apiClient, type ApiEnvelope } from "./client";

export interface MockMatchRuleDto {
  id?: string;
  source: "Header" | "QueryString" | "Path" | "RequestBody";
  field: string;
  operator: "Equals" | "NotEquals" | "Contains" | "StartsWith";
  expectedValue: string;
}

export interface MockResponseDto {
  id?: string;
  name: string;
  httpStatusCode: number;
  responseHeaders?: string | null;
  responseBody?: string | null;
  delayMilliseconds: number;
  isActive: boolean;
  priority: number;
  matchRules: MockMatchRuleDto[];
}

export interface MockEndpointDto {
  id?: string;
  path: string;
  httpMethod: string;
  isActive: boolean;
  responses: MockResponseDto[];
}

export interface MockApiDto {
  id?: string;
  code: string;
  name: string;
  description?: string | null;
  environment: string;
  isActive: boolean;
  endpoints: MockEndpointDto[];
}

export async function listMockApis(): Promise<MockApiDto[]> {
  const response = await apiClient.get<ApiEnvelope<MockApiDto[]>>("/mock-admin/apis");
  return response.data.data ?? [];
}

export async function upsertMockApi(code: string, dto: MockApiDto): Promise<MockApiDto> {
  const response = await apiClient.put<ApiEnvelope<MockApiDto>>(`/mock-admin/apis/${encodeURIComponent(code)}`, dto);
  return response.data.data!;
}

export async function deleteMockApi(code: string): Promise<void> {
  await apiClient.delete(`/mock-admin/apis/${encodeURIComponent(code)}`);
}

export async function enableMockApi(code: string): Promise<MockApiDto> {
  const response = await apiClient.post<ApiEnvelope<MockApiDto>>(`/mock-admin/apis/${encodeURIComponent(code)}/enable`);
  return response.data.data!;
}

export async function disableMockApi(code: string): Promise<MockApiDto> {
  const response = await apiClient.post<ApiEnvelope<MockApiDto>>(`/mock-admin/apis/${encodeURIComponent(code)}/disable`);
  return response.data.data!;
}

export function emptyMockApi(code: string, environment: string): MockApiDto {
  return { code, name: code, description: "", environment, isActive: false, endpoints: [] };
}

export function emptyEndpoint(): MockEndpointDto {
  return { path: "", httpMethod: "GET", isActive: true, responses: [] };
}

export function emptyResponse(priority: number): MockResponseDto {
  return {
    name: "Default",
    httpStatusCode: 200,
    responseHeaders: '{"Content-Type":"application/json"}',
    responseBody: "{}",
    delayMilliseconds: 0,
    isActive: true,
    priority,
    matchRules: [],
  };
}

export function emptyMatchRule(): MockMatchRuleDto {
  return { source: "Header", field: "", operator: "Equals", expectedValue: "" };
}
