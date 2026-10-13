"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { DemoButton } from "./DemoButton";
import { ErrorNotice, Notice, useAction } from "./ui";

export function AuthForm({ mode }: { mode: "login" | "register" }) {
  const router = useRouter();
  const params = useSearchParams();
  const { run, busy, error } = useAction();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const isRegister = mode === "register";

  async function submit(event: FormEvent) {
    event.preventDefault();
    const done = await run(async () => {
      if (isRegister) await api.auth.register({ email, password, firstName, lastName });
      else await api.auth.login(email, password);
      return true;
    });
    if (done) router.push("/board");
  }

  return (
    <div className="auth">
      <div className="eyebrow">{isRegister ? "Create an account" : "Welcome back"}</div>
      <h1 style={{ fontSize: 56, marginBottom: 20 }}>{isRegister ? "Start your notebook" : "Sign in"}</h1>

      {params.get("error") === "unreachable" && (
        <Notice kind="error">We could not reach the server. Try again in a moment.</Notice>
      )}

      <form className="form panel" onSubmit={submit} aria-label={isRegister ? "Create account" : "Sign in"}>
        {isRegister && (
          <div className="pair">
            <label>
              <span>First name</span>
              <input type="text" value={firstName} onChange={(e) => setFirstName(e.target.value)} required maxLength={80} autoComplete="given-name" />
            </label>
            <label>
              <span>Last name</span>
              <input type="text" value={lastName} onChange={(e) => setLastName(e.target.value)} required maxLength={80} autoComplete="family-name" />
            </label>
          </div>
        )}
        <label>
          <span>Email</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required maxLength={150} autoComplete="email" />
        </label>
        <label>
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={isRegister ? 10 : undefined}
            maxLength={128}
            autoComplete={isRegister ? "new-password" : "current-password"}
            aria-describedby={isRegister ? "password-hint" : undefined}
          />
          {isRegister && <div id="password-hint" className="hint">At least 10 characters.</div>}
        </label>

        <ErrorNotice error={error} />
        <button type="submit" className="btn primary" disabled={busy}>
          {busy ? "One moment…" : isRegister ? "Create account" : "Sign in"}
        </button>
        <p className="note">
          {isRegister ? (
            <>Already have an account? <Link href="/login">Sign in</Link></>
          ) : (
            <>New here? <Link href="/register">Create an account</Link></>
          )}
        </p>
      </form>

      <div className="ctas">
        <DemoButton className="btn">Or try the demo with sample data</DemoButton>
      </div>
    </div>
  );
}
