import type { FormEvent } from "react";
import { authRequest } from "./auth";
import type { AuthPageMode } from "./AuthPage";
import AuthNavigation from "./AuthNavigation";
import { useAuthForm } from "./useAuthForm";
import { PasswordRules } from "./PasswordRules";
interface Props {
  onNavigate: (page: AuthPageMode) => void;
  onRegistered: (email: string) => void;
}
export default function RegisterPage({ onNavigate, onRegistered }: Props) {
  const form = useAuthForm(true);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (form.busy) return;
    form.setApiError("");
    if (!form.validate()) return;
    form.setBusy(true);
    try {
      await authRequest("register", {
        name: form.values.name.trim(),
        email: form.values.email.trim(),
        password: form.values.password,
      });
      onRegistered(form.values.email.trim());
    } catch (error) {
      form.setApiError(
        error instanceof Error
          ? error.message
          : "Unable to create your account.",
      );
    } finally {
      form.setBusy(false);
    }
  }
  return (
    <>
      <AuthNavigation
        page="register"
        onNavigate={(page) => {
          if (!form.busy) onNavigate(page);
        }}
      />
      <h2 id="form-title">Start here.</h2>
      <p className="subtitle">
        Create your account and start organizing your finances.
      </p>
      <form noValidate onSubmit={submit}>
        {form.field("name", "Name", "text", "name", "Your name")}
        {form.field("email", "Email", "email", "email", "name@example.com")}
        {form.field(
          "password",
          "Password",
          "password",
          "new-password",
          "Create a password",
        )}
        <PasswordRules password={form.values.password} />
        {form.field(
          "confirm",
          "Confirm password",
          "password",
          "new-password",
          "Repeat your password",
        )}
        <button className="submit" disabled={form.busy}>
          {form.busy ? "Processing..." : "Create account"}
        </button>
        {form.apiError && (
          <p className="error" role="alert">
            {form.apiError}
          </p>
        )}
      </form>
      <p className="alternate">
        Already have an account?{" "}
        <a
          href="#login"
          onClick={(event) => {
            event.preventDefault();
            if (!form.busy) onNavigate("login");
          }}
        >
          Sign in
        </a>
      </p>
    </>
  );
}
