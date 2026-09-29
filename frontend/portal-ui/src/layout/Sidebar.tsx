import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  Toolbar,
  Box,
} from "@mui/material";
import { NavLink, useLocation } from "react-router-dom";
import { navSections } from "./navConfig";
import { useAuth } from "../auth/useAuth";

const DRAWER_WIDTH = 260;

export function Sidebar() {
  const location = useLocation();
  const { hasPermission } = useAuth();

  const visibleSections = navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => !item.permission || hasPermission(item.permission)),
    }))
    .filter((section) => section.items.length > 0);

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        [`& .MuiDrawer-paper`]: {
          width: DRAWER_WIDTH,
          boxSizing: "border-box",
          borderRight: "1px solid",
          borderColor: "divider",
        },
      }}
    >
      <Toolbar />
      <Box sx={{ overflowY: "auto", py: 1 }}>
        {visibleSections.map((section, index) => (
          <Box key={section.title ?? `section-${index}`} sx={{ mb: 1 }}>
            {section.title && (
              <Typography
                variant="body2"
                sx={{
                  px: 2.5,
                  pt: 2,
                  pb: 0.5,
                  color: "text.secondary",
                  fontWeight: 700,
                  letterSpacing: 0.5,
                  textTransform: "uppercase",
                  fontSize: "0.7rem",
                }}
              >
                {section.title}
              </Typography>
            )}
            <List disablePadding>
              {section.items.map((item) => {
                const isActive = location.pathname === item.path;
                const Icon = item.icon;
                return (
                  <ListItemButton
                    key={item.path}
                    component={NavLink}
                    to={item.path}
                    selected={isActive}
                    sx={{
                      mx: 1,
                      borderRadius: 2,
                      "&.Mui-selected": {
                        backgroundColor: "primary.main",
                        color: "primary.contrastText",
                        "& .MuiListItemIcon-root": { color: "primary.contrastText" },
                        "&:hover": { backgroundColor: "primary.dark" },
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 36 }}>
                      <Icon fontSize="small" />
                    </ListItemIcon>
                    <ListItemText slotProps={{ primary: { variant: "body2" } }}>
                      {item.label}
                    </ListItemText>
                  </ListItemButton>
                );
              })}
            </List>
          </Box>
        ))}
      </Box>
    </Drawer>
  );
}

export { DRAWER_WIDTH };
