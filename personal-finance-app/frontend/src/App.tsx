import { useEffect, useState } from "react";
import AuthPage from "./pages/auth/AuthPage";
import type { AuthPageMode } from "./pages/auth/AuthPage";
import LoginPage from "./pages/auth/LoginPage";
import RegisterPage from "./pages/auth/RegisterPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import SessionPage from "./pages/auth/SessionPage";
import { authRequest } from "./pages/auth/auth";
import type { User } from "./pages/auth/auth";

function getRoute(): AuthPageMode {
  if (
    window.location.hash === "#register" ||
    window.location.hash === "#registo"
  )
    return "register";
  if (window.location.hash === "#forgot-password") return "forgot-password";
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
        <SessionPage
          user={user}
          onLogout={() => {
            setUser(null);
            navigate("login");
            setLoginNotice({ email: "", message: "Signed out successfully." });
          }}
        />
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
      ) : (
        <LoginPage
          onNavigate={navigate}
          onLogin={setUser}
          email={loginNotice.email}
          message={loginNotice.message}
        />
      )}
    </AuthPage>
  );
}
