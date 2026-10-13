"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { useApp } from "@/lib/app-context";
import { timeLeft } from "@/lib/format";

const NAV = [
  { href: "/board", label: "Board", match: ["/board", "/applications"] },
  { href: "/today", label: "Today", match: ["/today"] },
  { href: "/insights", label: "Insights", match: ["/insights"] },
  { href: "/offers", label: "Offers", match: ["/offers"] },
  { href: "/stories", label: "Stories", match: ["/stories"] },
];

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { profile, todayCount, logout } = useApp();

  return (
    <>
      <a href="#main" className="skip-link">Skip to content</a>
      <header className="top">
        <div className="wrap">
          <Link className="brand" href="/board"><i aria-hidden="true"></i>JobHuntly</Link>
          <nav className="nav" aria-label="Main">
            {NAV.map((item) => {
              const current = item.match.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
              return (
                <Link key={item.href} href={item.href} aria-current={current ? "page" : undefined}>
                  {item.label}
                  {item.href === "/today" && todayCount > 0 && (
                    <span className="count" aria-label={`${todayCount} items need you`}>{todayCount}</span>
                  )}
                </Link>
              );
            })}
          </nav>
          <span className="who">
            {profile.isDemo && profile.demoExpiresAt
              ? `demo sandbox · expires in ${timeLeft(profile.demoExpiresAt)}`
              : profile.email}
          </span>
          <button type="button" className="btn small" onClick={logout}>Log out</button>
        </div>
      </header>
      <main id="main" className="main">{children}</main>
    </>
  );
}
