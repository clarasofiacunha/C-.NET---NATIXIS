import type { FormEvent } from "react";
import { authRequest } from "./auth";
import type { User } from "./auth";
import type { AuthPageMode } from "./AuthPage";
import AuthNavigation from "./AuthNavigation";
import { useAuthForm } from "./useAuthForm";
interface Props {
  onNavigate: (page: AuthPageMode) => void;
  onLogin: (user: User) => void;
  email?: string;
  message?: string;
}
export default function LoginPage({
  onNavigate,
  onLogin,
  email,
  message,
}: Props) {
  const form = useAuthForm(false, email);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (form.busy) return;
    form.setApiError("");
    if (!form.validate()) return;
    form.setBusy(true);
    try {
      onLogin(
        await authRequest<User>("login", {
          email: form.values.email.trim(),
          password: form.values.password,
        }),
      );
    } catch (error) {
      form.setApiError(
        error instanceof Error ? error.message : "Unable to sign in.",
      );
    } finally {
      form.setBusy(false);
    }
  }
  return (
    <>
      <AuthNavigation
        page="login"
        onNavigate={(page) => {
          if (!form.busy) onNavigate(page);
        }}
      />
      <h2 id="form-title">Welcome back.</h2>
      <p className="subtitle">Sign in to keep track of your finances.</p>
      {message && (
        <p className="success" role="status">
          {message}
        </p>
      )}
      <form noValidate onSubmit={submit}>
        {form.field("email", "Email", "email", "email", "name@example.com")}
        {form.field(
          "password",
          "Password",
          "password",
          "current-password",
          "Your password",
        )}
        <button
          className="forgot-password"
          type="button"
          disabled={form.busy}
          onClick={() => onNavigate("forgot-password")}
        >
          Forgot password?
        </button>
        <button className="submit" disabled={form.busy}>
          {form.busy ? "Processing..." : "Sign in"}
        </button>
        {form.apiError && (
          <p className="error" role="alert">
            {form.apiError}
          </p>
        )}
      </form>
      <p className="alternate">
        Don't have an account yet?{" "}
        <a
          href="#register"
          onClick={(event) => {
            event.preventDefault();
            if (!form.busy) onNavigate("register");
          }}
        >
          Create account
        </a>
      </p>
    </>
  );
}
