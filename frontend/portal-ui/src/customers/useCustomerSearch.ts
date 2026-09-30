import { z } from "zod";
import { useMutation } from "@tanstack/react-query";
import { AxiosError } from "axios";
import { apiClient, type ApiEnvelope } from "../api/client";
import type { Customer } from "./customerTypes";

export const customerSearchSchema = z.object({
  mobileNumber: z
    .string()
    .min(1, "Mobile number is required")
    .regex(/^[0-9]{7,15}$/, "Enter 7-15 digits, numbers only"),
});

export type CustomerSearchValues = z.infer<typeof customerSearchSchema>;

/** Exact mobile-number lookup behind the CustomerLookupBar on every customer screen. */
export function useCustomerSearch() {
  const mutation = useMutation({
    mutationFn: async (mobileNumber: string) => {
      const response = await apiClient.get<ApiEnvelope<Customer>>("/customers/search", {
        params: { mobileNumber },
      });
      return response.data.data!;
    },
  });

  const errorMessage = mutation.isError
    ? ((mutation.error as AxiosError<ApiEnvelope<unknown>>).response?.data?.error?.message ??
      "Something went wrong while searching. Please try again.")
    : null;

  return { ...mutation, errorMessage };
}
