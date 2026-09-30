import { useQuery } from "@tanstack/react-query";
import { apiClient, type ApiEnvelope } from "../../api/client";
import type { ScreenDefinition } from "./types";

export function useScreenDefinition(screenCode: string, enabled = true) {
  return useQuery({
    queryKey: ["screen-definition", screenCode],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<ScreenDefinition>>(`/screens/${screenCode}`);
      return response.data.data!;
    },
    staleTime: 5 * 60 * 1000,
    enabled,
  });
}
