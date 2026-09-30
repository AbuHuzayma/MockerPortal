import { BrowserRouter, Routes, Route } from "react-router-dom";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { theme } from "./theme/theme";
import { AppLayout } from "./layout/AppLayout";
import { navSections } from "./layout/navConfig";
import { AuthProvider } from "./auth/AuthContext";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { CustomerProvider } from "./customers/CustomerContext";
import { CustomerWorkspace } from "./customers/CustomerWorkspace";
import { MerchantProvider } from "./merchants/MerchantContext";
import { MerchantWorkspace } from "./merchants/MerchantWorkspace";
import { LoginPage } from "./pages/LoginPage";
import { ForbiddenPage } from "./pages/ForbiddenPage";
import { DashboardPage } from "./pages/DashboardPage";
import { CustomerOverviewPage } from "./pages/CustomerOverviewPage";
import { KycPage } from "./pages/KycPage";
import { IvrPage } from "./pages/IvrPage";
import { CreationPage } from "./pages/CreationPage";
import { OtpPage } from "./pages/OtpPage";
import { CardsPage } from "./pages/CardsPage";
import { BeneficiaryPage } from "./pages/BeneficiaryPage";
import { SecurityPage } from "./pages/SecurityPage";
import { BiometricPage } from "./pages/BiometricPage";
import { MerchantProfilePage } from "./pages/MerchantProfilePage";
import { MerchantB2BPage } from "./pages/MerchantB2BPage";
import { SampleScreenPage } from "./pages/SampleScreenPage";
import { ApiMockerPage } from "./pages/ApiMockerPage";
import { AuditPage } from "./pages/AuditPage";
import { AdminPage } from "./pages/AdminPage";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: false },
  },
});

// Every screen here is rendered inside CustomerWorkspace / MerchantWorkspace
// (lookup bar on top) and gated by its nav item's permission.
const customerScreens: { path: string; element: React.ReactNode }[] = [
  { path: "/customers/overview", element: <CustomerOverviewPage /> },
  { path: "/customers/kyc", element: <KycPage /> },
  { path: "/customers/ivr", element: <IvrPage /> },
  { path: "/customers/creation", element: <CreationPage /> },
  { path: "/customers/otp", element: <OtpPage /> },
  { path: "/customers/cards", element: <CardsPage /> },
  { path: "/customers/beneficiary", element: <BeneficiaryPage /> },
  { path: "/customers/security", element: <SecurityPage /> },
  { path: "/customers/biometrics", element: <BiometricPage /> },
];

const merchantScreens: { path: string; element: React.ReactNode }[] = [
  { path: "/merchants", element: <MerchantProfilePage /> },
  { path: "/merchants/b2b", element: <MerchantB2BPage /> },
];

const apiMockerPages: { path: string; title: string; codes: string[] | "other"; defaultCodePrefix: string }[] = [
  { path: "/api-mocker/absher", title: "API Mocker — Absher", codes: ["ABSHER"], defaultCodePrefix: "ABSHER" },
  { path: "/api-mocker/yakeen", title: "API Mocker — Yakeen", codes: ["YAKEEN"], defaultCodePrefix: "YAKEEN" },
  { path: "/api-mocker/elm", title: "API Mocker — ELM", codes: ["ELM"], defaultCodePrefix: "ELM" },
  { path: "/api-mocker/other", title: "API Mocker — Other APIs", codes: "other", defaultCodePrefix: "" },
];

const permissionByPath = new Map(
  navSections.flatMap((section) => section.items.map((item) => [item.path, item.permission])),
);

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <AuthProvider>
            <CustomerProvider>
              <MerchantProvider>
                <Routes>
                  <Route path="/login" element={<LoginPage />} />

                  <Route element={<ProtectedRoute />}>
                    <Route element={<AppLayout />}>
                      <Route path="/" element={<DashboardPage />} />
                      <Route path="/forbidden" element={<ForbiddenPage />} />
                      <Route path="/dev/sample-screen" element={<SampleScreenPage />} />

                      <Route element={<CustomerWorkspace />}>
                        {customerScreens.map(({ path, element }) => (
                          <Route key={path} element={<ProtectedRoute permission={permissionByPath.get(path)} />}>
                            <Route path={path} element={element} />
                          </Route>
                        ))}
                      </Route>

                      <Route element={<MerchantWorkspace />}>
                        {merchantScreens.map(({ path, element }) => (
                          <Route key={path} element={<ProtectedRoute permission={permissionByPath.get(path)} />}>
                            <Route path={path} element={element} />
                          </Route>
                        ))}
                      </Route>

                      {apiMockerPages.map(({ path, title, codes, defaultCodePrefix }) => (
                        <Route key={path} element={<ProtectedRoute permission={permissionByPath.get(path)} />}>
                          <Route
                            path={path}
                            element={<ApiMockerPage title={title} codes={codes} defaultCodePrefix={defaultCodePrefix} />}
                          />
                        </Route>
                      ))}

                      <Route element={<ProtectedRoute permission={permissionByPath.get("/audit")} />}>
                        <Route path="/audit" element={<AuditPage />} />
                      </Route>

                      <Route element={<ProtectedRoute permission={permissionByPath.get("/admin")} />}>
                        <Route path="/admin" element={<AdminPage />} />
                      </Route>
                    </Route>
                  </Route>
                </Routes>
              </MerchantProvider>
            </CustomerProvider>
          </AuthProvider>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
