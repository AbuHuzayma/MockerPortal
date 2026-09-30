import DashboardIcon from "@mui/icons-material/DashboardOutlined";
import PersonSearchIcon from "@mui/icons-material/PersonSearchOutlined";
import BadgeIcon from "@mui/icons-material/BadgeOutlined";
import PhoneIcon from "@mui/icons-material/PhoneInTalkOutlined";
import EventIcon from "@mui/icons-material/EventOutlined";
import PinIcon from "@mui/icons-material/PinOutlined";
import CreditCardIcon from "@mui/icons-material/CreditCardOutlined";
import GroupIcon from "@mui/icons-material/GroupOutlined";
import LockOpenIcon from "@mui/icons-material/LockOpenOutlined";
import FingerprintIcon from "@mui/icons-material/FingerprintOutlined";
import StorefrontIcon from "@mui/icons-material/StorefrontOutlined";
import HubIcon from "@mui/icons-material/HubOutlined";
import ApiIcon from "@mui/icons-material/ApiOutlined";
import HistoryIcon from "@mui/icons-material/HistoryOutlined";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettingsOutlined";
import type { SvgIconComponent } from "@mui/icons-material";

export interface NavItem {
  label: string;
  path: string;
  icon: SvgIconComponent;
  /** Permission required to see this item — enforced for real once Phase 1 auth ships. */
  permission?: string;
}

export interface NavSection {
  title: string | null;
  items: NavItem[];
}

export const navSections: NavSection[] = [
  {
    title: null,
    items: [{ label: "Dashboard", path: "/", icon: DashboardIcon }],
  },
  {
    title: "Customer",
    items: [
      { label: "Overview", path: "/customers/overview", icon: PersonSearchIcon, permission: "customer.view" },
      { label: "KYC", path: "/customers/kyc", icon: BadgeIcon, permission: "customer.kyc.view" },
      { label: "IVR", path: "/customers/ivr", icon: PhoneIcon, permission: "customer.ivr.view" },
      { label: "Creation", path: "/customers/creation", icon: EventIcon, permission: "customer.creation.view" },
      { label: "OTP", path: "/customers/otp", icon: PinIcon, permission: "customer.otp.view" },
      { label: "Cards", path: "/customers/cards", icon: CreditCardIcon, permission: "customer.card.view" },
      { label: "Beneficiary", path: "/customers/beneficiary", icon: GroupIcon, permission: "customer.beneficiary.view" },
      { label: "Security", path: "/customers/security", icon: LockOpenIcon, permission: "customer.security.view" },
      { label: "Biometrics", path: "/customers/biometrics", icon: FingerprintIcon, permission: "customer.biometric.view" },
    ],
  },
  {
    title: "Merchant",
    items: [
      { label: "Merchant Details", path: "/merchants", icon: StorefrontIcon, permission: "merchant.view" },
      { label: "B2B", path: "/merchants/b2b", icon: HubIcon, permission: "merchant.b2b.view" },
    ],
  },
  {
    title: "API Mocker",
    items: [
      { label: "Absher", path: "/api-mocker/absher", icon: ApiIcon, permission: "api-mocker.view" },
      { label: "Yakeen", path: "/api-mocker/yakeen", icon: ApiIcon, permission: "api-mocker.view" },
      { label: "ELM", path: "/api-mocker/elm", icon: ApiIcon, permission: "api-mocker.view" },
      { label: "Other APIs", path: "/api-mocker/other", icon: ApiIcon, permission: "api-mocker.view" },
    ],
  },
  {
    title: "Audit",
    items: [{ label: "Audit Log", path: "/audit", icon: HistoryIcon, permission: "audit.view" }],
  },
  {
    title: "Admin",
    items: [{ label: "Administration", path: "/admin", icon: AdminPanelSettingsIcon, permission: "admin.users" }],
  },
];
