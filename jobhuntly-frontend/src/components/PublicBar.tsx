import Link from "next/link";

export function PublicBar({ action = "signin" }: { action?: "signin" | "none" }) {
  return (
    <header className="top">
      <div className="wrap">
        <Link className="brand" href="/"><i aria-hidden="true"></i>JobHuntly</Link>
        <div className="nav" />
        {action === "signin" && <Link className="btn small" href="/login">Sign in</Link>}
      </div>
    </header>
  );
}
