import { useEffect, useState } from "react";
import AuthPage from "./pages/auth/AuthPage";
import type { AuthPageMode } from "./pages/auth/AuthPage";
import LoginPage from "./pages/auth/LoginPage";
import RegisterPage from "./pages/auth/RegisterPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import ResetPasswordPage from "./pages/auth/ResetPasswordPage";
import DashboardPage from "./pages/dashboard/DashboardPage";
import EmptyPage from "./pages/dashboard/EmptyPage";
import AccountsPage from "./pages/dashboard/AccountsPage";
import SummaryPage from "./pages/dashboard/SummaryPage";
import BudgetsPage from "./pages/dashboard/BudgetsPage";
import { authRequest } from "./pages/auth/auth";
import type { User } from "./pages/auth/auth";

const APP_ROUTES: AuthPageMode[] = ["dashboard", "accounts", "summary", "budgets", "more"];

function getRoute(): AuthPageMode {
  if (
    window.location.hash === "#register" ||
    window.location.hash === "#registo"
  )
    return "register";
  if (window.location.hash === "#forgot-password") return "forgot-password";
  if (window.location.hash.startsWith("#reset-password")) return "reset-password";
  if (window.location.hash === "#dashboard") return "dashboard";
  if (["#accounts", "#summary", "#budgets", "#more"].includes(window.location.hash))
    return window.location.hash.slice(1) as AuthPageMode;
  return "login";
}

export default function App() {
  const [route, setRoute] = useState<AuthPageMode>(getRoute);
  const [user, setUser] = useState<User | null>(null);
  const [checking, setChecking] = useState(true);
  const [loginNotice, setLoginNotice] = useState({ email: "", message: "" });
  useEffect(() => {
    const syncRoute = () => {
      setRoute(getRoute());
      setLoginNotice({ email: "", message: "" });
    };
    window.addEventListener("hashchange", syncRoute);
    return () => window.removeEventListener("hashchange", syncRoute);
  }, []);
  useEffect(() => {
    let active = true;
    authRequest<User>("me")
      .then((current) => {
        if (active) setUser(current);
      })
      .catch(() => {
        /* No active session: show the authentication pages. */
      })
      .finally(() => {
        if (active) setChecking(false);
      });
    return () => {
      active = false;
    };
  }, []);
  // Com sessao ativa, as paginas de autenticacao (ex.: abrir a app sem "#")
  // mostram o dashboard; sem sessao, as paginas da app mostram o login.
  const isAppRoute = APP_ROUTES.includes(route);
  const page: AuthPageMode = checking
    ? route
    : user && !isAppRoute
      ? "dashboard"
      : !user && isAppRoute
        ? "login"
        : route;
  function navigate(next: AuthPageMode) {
    window.history.pushState(null, "", "#" + next);
    setRoute(next);
    setLoginNotice({ email: "", message: "" });
  }
  return (
    <AuthPage page={page} onNavigate={navigate}>
      {checking ? (
        <p id="form-title" role="status">
          Checking your session...
        </p>
      ) : user ? (
        <>
          {page === "dashboard" ? (
            <DashboardPage
              user={user}
              onNavigate={navigate}
              onLogout={() => {
                setUser(null);
                navigate("login");
                setLoginNotice({ email: "", message: "Signed out successfully." });
              }}
            />
          ) : page === "accounts" ? (
            <AccountsPage
              user={user}
              onNavigate={navigate}
              onLogout={() => {
                setUser(null);
                navigate("login");
                setLoginNotice({ email: "", message: "Signed out successfully." });
              }}
            />
          ) : page === "summary" ? (
            <SummaryPage
              user={user}
              onNavigate={navigate}
              onLogout={() => {
                setUser(null);
                navigate("login");
                setLoginNotice({ email: "", message: "Signed out successfully." });
              }}
            />
          ) : page === "budgets" ? (
            <BudgetsPage
              user={user}
              onNavigate={navigate}
              onLogout={() => {
                setUser(null);
                navigate("login");
                setLoginNotice({ email: "", message: "Signed out successfully." });
              }}
            />
          ) : page === "more" ? (
            <EmptyPage
              page={page}
              user={user}
              onNavigate={navigate}
              onLogout={() => {
                setUser(null);
                navigate("login");
                setLoginNotice({ email: "", message: "Signed out successfully." });
              }}
            />
          ) : null}
        </>
      ) : page === "register" ? (
        <RegisterPage
          onNavigate={navigate}
          onRegistered={(email) => {
            navigate("login");
            setLoginNotice({
              email,
              message: "Account created successfully. You can now sign in.",
            });
          }}
        />
      ) : page === "forgot-password" ? (
        <ForgotPasswordPage onNavigate={navigate} />
      ) : page === "reset-password" ? (
        <ResetPasswordPage onNavigate={navigate} />
      ) : (
        <LoginPage
          onNavigate={navigate}
          onLogin={(currentUser) => {
            setUser(currentUser);
            navigate("dashboard");
          }}
          email={loginNotice.email}
          message={loginNotice.message}
        />
      )}
    </AuthPage>
  );
}
