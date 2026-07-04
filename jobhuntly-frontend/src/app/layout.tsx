import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "JobHuntly | Swiss Career Tracker",
  description: "Track your job applications with style.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
