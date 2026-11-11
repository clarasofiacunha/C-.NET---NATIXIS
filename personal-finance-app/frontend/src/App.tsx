import { useEffect, useState } from "react";
import AuthPage from "./pages/auth/AuthPage";
import type { AuthPageMode } from "./pages/auth/AuthPage";
import LoginPage from "./pages/auth/LoginPage";
import RegisterPage from "./pages/auth/RegisterPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import ResetPasswordPage from "./pages/auth/ResetPasswordPage";
import DashboardPage from "./pages/dashboard/DashboardPage";
import EmptyPage from "./pages/dashboard/EmptyPage";
import { authRequest } from "./pages/auth/auth";
import type { User } from "./pages/auth/auth";

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
  function navigate(next: AuthPageMode) {
    window.history.pushState(null, "", "#" + next);
    setRoute(next);
    setLoginNotice({ email: "", message: "" });
  }
  return (
    <AuthPage page={route} onNavigate={navigate}>
      {checking ? (
        <p id="form-title" role="status">
          Checking your session...
        </p>
      ) : user ? (
        <>
          {route === "dashboard" ? (
            <DashboardPage
              user={user}
              onNavigate={navigate}
              onLogout={() => {
                setUser(null);
                navigate("login");
                setLoginNotice({ email: "", message: "Signed out successfully." });
              }}
            />
          ) : route === "accounts" || route === "summary" || route === "budgets" || route === "more" ? (
            <EmptyPage
              page={route}
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
      ) : route === "register" ? (
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
      ) : route === "forgot-password" ? (
        <ForgotPasswordPage onNavigate={navigate} />
      ) : route === "reset-password" ? (
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
