import type { Metadata } from "next";
import { Suspense } from "react";
import { AuthForm } from "@/components/AuthForm";
import { PublicBar } from "@/components/PublicBar";

export const metadata: Metadata = { title: "Sign in" };

export default function LoginPage() {
  return (
    <>
      <PublicBar action="none" />
      <main id="main" className="wrap">
        <Suspense fallback={null}>
          <AuthForm mode="login" />
        </Suspense>
      </main>
    </>
  );
}
