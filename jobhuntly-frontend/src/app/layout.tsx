import type { Metadata } from "next";
import { Barlow, Barlow_Condensed, IBM_Plex_Mono } from "next/font/google";
import "./globals.css";

const display = Barlow_Condensed({ weight: ["600", "700"], subsets: ["latin"], variable: "--font-display", display: "swap" });
const text = Barlow({ weight: ["400", "500", "600"], subsets: ["latin"], variable: "--font-text", display: "swap" });
const mono = IBM_Plex_Mono({ weight: ["400", "500"], subsets: ["latin"], variable: "--font-mono", display: "swap" });

export const metadata: Metadata = {
  title: { default: "JobHuntly: the interview notebook", template: "%s · JobHuntly" },
  description:
    "Prepare each interview, debrief right after, and see what your job search is really telling you. Try it with sample data, no signup.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${display.variable} ${text.variable} ${mono.variable}`}>
      <body>{children}</body>
    </html>
  );
}
