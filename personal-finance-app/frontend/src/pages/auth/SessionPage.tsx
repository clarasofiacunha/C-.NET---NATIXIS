import { useState } from "react";
import { authRequest } from "./auth";
import type { User } from "./auth";
export default function SessionPage({
  user,
  onLogout,
}: {
  user: User;
  onLogout: () => void;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function logout() {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await authRequest<void>("logout");
      onLogout();
    } catch (error) {
      setError(error instanceof Error ? error.message : "Unable to sign out.");
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <h2 id="form-title">Hello, {user.name}.</h2>
      <p className="success" role="status">
        Signed in successfully.
      </p>
      <p className="subtitle">{user.email}</p>
      <button className="submit" disabled={busy} onClick={logout}>
        {busy ? "Signing out..." : "Sign out"}
      </button>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </>
  );
}
