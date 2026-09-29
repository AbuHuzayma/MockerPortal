# 11 — UI Design System (STC Bank-inspired)

## 1. Brand direction

Modern enterprise/fintech look inspired by the STC Bank visual identity —
**not** a pixel copy of any proprietary screen. Purple-led palette, light
surfaces, dark text, rounded cards, generous but efficient spacing, high
information density without clutter.

Official STC Bank brand assets/exact hex values are **not yet supplied** —
the palette below is a documented placeholder using CSS/MUI theme tokens
specifically so it can be swapped later without touching component code.

## 2. Theme structure

```
frontend/portal-ui/src/theme/
  theme.ts          -- createTheme(...) composing the pieces below
  colors.ts          -- color tokens (placeholder palette, swap here only)
  typography.ts        -- font family/scale
  components.ts          -- MUI component style overrides (buttons, cards, inputs)
```

No component file hardcodes a hex color or raw spacing value — everything
references `theme.palette.*` / `theme.spacing()`.

## 3. Placeholder palette (swap when brand assets are supplied)

| Token | Value (placeholder) | Usage |
|---|---|---|
| `primary.main` | `#5B2A86` (STC purple, placeholder) | Primary actions, active nav, links |
| `primary.dark` | `#3E1C5E` | Hover/pressed states, header |
| `background.default` | `#F5F4F8` | App background |
| `background.paper` | `#FFFFFF` | Cards, panels |
| `text.primary` | `#1E1B22` | Body text |
| `text.secondary` | `#6B6572` | Muted text |
| `success.main` | `#2E7D32` | Success states |
| `warning.main` | `#ED6C02` | Warnings |
| `error.main` | `#D32F2F` | Errors, destructive actions |
| `info.main` | `#0288D1` | Informational |

Environment badge colors (distinct from the above, intentionally loud):

| Environment | Color |
|---|---|
| DEV | Gray `#6B6572` |
| QA | Blue `#0288D1` |
| PREPROD | Amber/Red `#ED6C02` |

## 4. Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Header: logo + portal name        user menu | ENV badge      │
├──────────────┬──────────────────────────────────────────────┤
│ Sidebar nav  │  Main content area                            │
│  Dashboard   │   - breadcrumb                                │
│  CUSTOMER     │   - page title + actions                     │
│    Search      │   - content (cards, tables, forms)           │
│    KYC ...       │                                            │
│  MERCHANT          │                                          │
│  API MOCKER          │                                        │
│  AUDIT                 │                                      │
│  ADMIN                   │                                    │
└──────────────┴──────────────────────────────────────────────┘
```

- Sidebar sections match the master spec's grouping exactly: **Dashboard,
  CUSTOMER** (Search, KYC, IVR, Creation, OTP, Cards, Beneficiary, Security,
  Biometrics, Onboarding), **MERCHANT** (Merchant, B2B), **API MOCKER**
  (Absher, Yakeen, ELM, Other APIs), **AUDIT**, **ADMIN**.
- Sidebar items are filtered to the current user's `*.view` permissions —
  hidden, not just disabled, for screens the user cannot access at all.
- The environment badge is always visible in the header, uses the loud
  color table above, and is never abbreviated ambiguously — full text
  (`QA`, `PREPROD`) always shown, never just a color chip alone.

## 5. Mutation confirmation pattern

Every mutating action, before executing, shows:

```
┌───────────────────────────────────────────┐
│ Confirm: Update KYC — Email                │
│                                             │
│ Target: Customer 100234 (mobile ***1234)   │
│ Environment: QA                            │
│ Field          Current         New          │
│ EMAIL          old@x.com       new@x.com    │
│                                             │
│           [ Cancel ]   [ Confirm ]          │
└───────────────────────────────────────────┘
```

For higher-risk actions (security lock removal in PREPROD), the confirm
button is disabled until the user re-types the customer identifier or a
short confirmation phrase — matching the master spec's "explicit
confirmation" and "display fields that will change" requirements.

After execution, a result panel/toast shows `SUCCESS` or `FAILED` plus the
`correlationId`, so it can be quoted when reporting an issue.

## 6. Component conventions

- Cards: rounded corners (`borderRadius: 12` theme default), subtle
  elevation (1), no heavy borders.
- Tables: dense mode by default (QA/Dev audience values density over
  whitespace), sticky header, column sort where relevant.
- Forms: label above field, inline validation messages, disabled submit
  until valid, clear required-field indication.
- Empty/loading/error states are explicit components (`EmptyState`,
  `ErrorState`), never a blank screen.

## 7. Accessibility

- Minimum WCAG AA contrast for text/background combinations in the palette
  above (validated when real brand colors are supplied).
- All interactive elements keyboard-reachable; confirmation dialogs trap
  focus.
- Icons paired with text labels in navigation (not icon-only).

## 8. Responsiveness

Primary target is enterprise desktop (≥1280px). Layout remains usable down
to tablet width (collapsible sidebar) but is not optimized for phone-sized
screens — this is an internal desktop tool.
