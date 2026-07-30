import { Link, Outlet, useCanGoBack, useNavigate, useRouterState } from "@tanstack/react-router";
import { useCallback, useState } from "react";
import {
  Button,
  Separator,
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarTrigger,
  TooltipProvider,
} from "@apesdb/ui";
import { ArrowLeft, CalendarDays, Columns3, Gamepad2 } from "lucide-react";
import { useAuth } from "../../auth-context";
import { AccountMenu } from "../../account-menu";
import { AppBreadcrumbs } from "../../app-breadcrumbs";
import { NotificationBell } from "../notifications/notification-bell";
import { useNotificationStream } from "../notifications/use-notification-stream";

export function AppLayout() {
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const openNotifications = useCallback(() => {
    setNotificationsOpen(true);
  }, []);

  useNotificationStream(openNotifications);

  return (
    <TooltipProvider>
      <SidebarProvider className="h-svh overflow-hidden">
        <AppSidebar />
        <SidebarInset>
          <header className="flex h-12 shrink-0 items-center gap-2 border-b px-4">
            <SidebarTrigger className="-ml-1" size="icon-lg" />
            <Separator className="h-4" orientation="vertical" />
            <AppBreadcrumbs />
            <div className="ml-auto flex items-center">
              <NotificationBell open={notificationsOpen} onOpenChange={setNotificationsOpen} />
            </div>
          </header>
          <div className="flex min-h-0 flex-1 flex-col overflow-y-auto p-3">
            <AppBackButton />
            <div className="min-h-0 flex-1">
              <Outlet />
            </div>
          </div>
        </SidebarInset>
      </SidebarProvider>
    </TooltipProvider>
  );
}

function AppBackButton() {
  const canGoBack = useCanGoBack();
  const navigate = useNavigate();
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const pathSegments = pathname.split("/").filter(Boolean);
  const goBack = useCallback(() => {
    if (canGoBack) {
      window.history.back();
      return;
    }

    void navigate({ to: "/", replace: true });
  }, [canGoBack, navigate]);

  if (pathSegments.length <= 1) {
    return null;
  }

  return (
    <Button className="mb-3 shrink-0 self-start" type="button" variant="ghost" onClick={goBack}>
      <ArrowLeft />
      Back
    </Button>
  );
}

function AppSidebar() {
  const { user, logout } = useAuth();
  const pathname = useRouterState({ select: (state) => state.location.pathname });

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <Link
          className="flex h-12 items-center gap-2 border bg-sidebar-accent/50 px-2 group-data-[collapsible=icon]:size-8 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-0"
          to="/"
        >
          <img
            alt="ApesDb"
            className="size-8 shrink-0 group-data-[collapsible=icon]:size-6"
            src="/192x192.png"
          />
          <div className="min-w-0 leading-tight group-data-[collapsible=icon]:hidden">
            <div className="truncate text-sm font-semibold">ApesDb</div>
            <div className="truncate text-xs text-sidebar-foreground/70">The AperBase</div>
          </div>
        </Link>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel className="text-primary">General</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  render={<Link to="/games" />}
                  isActive={pathname.startsWith("/games")}
                  tooltip="Games"
                >
                  <Gamepad2 />
                  <span>Games</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  render={<Link to="/boards" />}
                  isActive={pathname.startsWith("/boards")}
                  tooltip="Boards"
                >
                  <Columns3 />
                  <span>Boards</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  render={<Link to="/calendar" />}
                  isActive={pathname.startsWith("/calendar")}
                  tooltip="Calendar"
                >
                  <CalendarDays />
                  <span>Calendar</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <AccountMenu user={user} onLogout={() => void logout()} />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
