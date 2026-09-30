import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  Stack,
  Typography,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { apiClient, type ApiEnvelope } from "../api/client";
import { useAuth } from "../auth/useAuth";
import { useCustomerContext } from "../customers/useCustomerContext";
import { DEFAULT_BENEFICIARY_ID, type Customer } from "../customers/customerTypes";
import { ReadOnlyFieldGrid } from "../components/dynamic-form/ReadOnlyFieldGrid";
import { useScreenDefinition } from "../components/dynamic-form/useScreenDefinition";
import type { FormValues } from "../components/dynamic-form/types";

interface OverviewSection {
  title: string;
  screenCode: string;
  permission: string;
  /** Same query key as the matching update screen, so the two share cached data. */
  queryKey: string;
  endpoint: (customerId: string) => string;
  /** The update screen for this data, if one exists. */
  editPath?: string;
}

const sections: OverviewSection[] = [
  { title: "KYC", screenCode: "CUSTOMER_KYC", permission: "customer.kyc.view", queryKey: "kyc", endpoint: (id) => `/customers/${id}/kyc`, editPath: "/customers/kyc" },
  { title: "IVR", screenCode: "CUSTOMER_IVR", permission: "customer.ivr.view", queryKey: "ivr", endpoint: (id) => `/customers/${id}/ivr`, editPath: "/customers/ivr" },
  { title: "Creation Dates", screenCode: "CUSTOMER_CREATION", permission: "customer.creation.view", queryKey: "creation", endpoint: (id) => `/customers/${id}/creation`, editPath: "/customers/creation" },
  { title: "OTP / IVR Cooling Period", screenCode: "CUSTOMER_OTP", permission: "customer.otp.view", queryKey: "otp-cooling", endpoint: (id) => `/customers/${id}/otp-cooling`, editPath: "/customers/otp" },
  { title: "Cards", screenCode: "CUSTOMER_CARDS", permission: "customer.card.view", queryKey: "cards", endpoint: (id) => `/customers/${id}/cards`, editPath: "/customers/cards" },
  { title: "Beneficiary", screenCode: "CUSTOMER_BENEFICIARY", permission: "customer.beneficiary.view", queryKey: "beneficiary", endpoint: (id) => `/customers/${id}/beneficiaries/${DEFAULT_BENEFICIARY_ID}`, editPath: "/customers/beneficiary" },
  { title: "Security", screenCode: "CUSTOMER_SECURITY", permission: "customer.security.view", queryKey: "security", endpoint: (id) => `/customers/${id}/security`, editPath: "/customers/security" },
  { title: "Biometrics", screenCode: "CUSTOMER_BIOMETRIC", permission: "customer.biometric.view", queryKey: "biometric", endpoint: (id) => `/customers/${id}/biometrics`, editPath: "/customers/biometrics" },
  { title: "Onboarding", screenCode: "CUSTOMER_ONBOARDING", permission: "customer.onboarding.view", queryKey: "onboarding", endpoint: (id) => `/customers/${id}/onboarding` },
];

function ProfileField({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value?.trim() ? value : "—"}</Typography>
    </Grid>
  );
}

/** Collapsed by default, and only fetches once expanded — some sections call external services. */
function OverviewSectionPanel({ section, customerId }: { section: OverviewSection; customerId: string }) {
  const [expanded, setExpanded] = useState(false);
  const screenQuery = useScreenDefinition(section.screenCode, expanded);
  const dataQuery = useQuery({
    queryKey: [section.queryKey, customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<FormValues>>(section.endpoint(customerId));
      return response.data.data!;
    },
    enabled: expanded,
  });

  return (
    <Accordion expanded={expanded} onChange={(_, isExpanded) => setExpanded(isExpanded)} disableGutters>
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Typography variant="h4">{section.title}</Typography>
      </AccordionSummary>
      <AccordionDetails>
        {(screenQuery.isLoading || dataQuery.isLoading) && <CircularProgress size={24} />}
        {(screenQuery.isError || dataQuery.isError) && (
          <Alert severity="error">Unable to load {section.title} details for this customer.</Alert>
        )}
        {screenQuery.data &&
          dataQuery.data &&
          (screenQuery.data.fields.length > 0 ? (
            <ReadOnlyFieldGrid fields={screenQuery.data.fields} values={dataQuery.data} />
          ) : (
            <Typography variant="body2" color="text.secondary">
              No fields are configured for this section yet.
            </Typography>
          ))}
        {section.editPath && (
          <Button component={RouterLink} to={section.editPath} variant="outlined" size="small" sx={{ mt: 2 }}>
            Open {section.title} screen
          </Button>
        )}
      </AccordionDetails>
    </Accordion>
  );
}

/** Read-only view of everything known about the active customer, one section per data area. */
export function CustomerOverviewPage() {
  const { customer: context } = useCustomerContext();
  const { hasPermission } = useAuth();

  const query = useQuery({
    queryKey: ["customer-profile", context?.customerId],
    queryFn: async () => {
      const response = await apiClient.get<ApiEnvelope<Customer>>(`/customers/${context!.customerId}`);
      return response.data.data!;
    },
    enabled: !!context,
  });

  if (!context) {
    return null; // CustomerWorkspace only renders this screen once a customer is selected.
  }

  return (
    <Stack spacing={2}>
      <Card>
        <CardContent>
          <Stack direction="row" sx={{ mb: 2, justifyContent: "space-between", alignItems: "flex-start" }}>
            <Typography variant="h3">Customer Overview</Typography>
            {query.data?.lifeStatus && <Chip label={query.data.lifeStatus} color="info" size="small" />}
          </Stack>

          {query.isLoading && <CircularProgress size={28} />}
          {query.isError && <Alert severity="error">Unable to load this customer's profile.</Alert>}

          {query.data && (
            <>
              <Divider sx={{ mb: 2 }} />
              <Grid container spacing={2}>
                <ProfileField label="Customer ID" value={query.data.custId} />
                <ProfileField label="Customer Number" value={query.data.custNumber} />
                <ProfileField label="T24 Customer ID" value={query.data.t24CustomerId} />
                <ProfileField label="Mobile Number" value={query.data.mobileNo} />
                <ProfileField label="First Name" value={query.data.firstName} />
                <ProfileField label="Last Name" value={query.data.lastName} />
                <ProfileField label="Arabic First Name" value={query.data.arabicFirstName} />
                <ProfileField label="Arabic Last Name" value={query.data.arabicLastName} />
                <ProfileField label="Email" value={query.data.email} />
                <ProfileField label="Nationality" value={query.data.nationality} />
                <ProfileField label="Life Status" value={query.data.lifeStatus} />
                <ProfileField label="Blacklist Status" value={query.data.blacklistStatus} />
              </Grid>
            </>
          )}
        </CardContent>
      </Card>

      <div>
        {sections
          .filter((section) => hasPermission(section.permission))
          .map((section) => (
            <OverviewSectionPanel key={section.queryKey} section={section} customerId={context.customerId} />
          ))}
      </div>
    </Stack>
  );
}
