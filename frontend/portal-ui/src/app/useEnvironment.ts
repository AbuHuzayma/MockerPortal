import { useQuery } from "@tanstack/react-query";
import { apiClient, type ApiEnvelope } from "../api/client";

export interface EnvironmentInfo {
  name: string;
  label: string;
  color: string;
}

export function useEnvironment() {
  return useQuery({
    queryKey: ["environment"],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<EnvironmentInfo>>("/meta/environment");
      return response.data.data as EnvironmentInfo;
    },
    staleTime: Infinity,
  });
}
