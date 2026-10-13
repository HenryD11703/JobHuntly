import type { NextConfig } from "next";

// The browser only ever talks to this app's origin. Requests to /api/* are forwarded to the backend
// on the server side, so the auth cookie the backend sets is a first-party cookie (no cross-site
// cookie problems) and the backend URL never reaches the browser.
const backend = (process.env.BACKEND_URL || "http://localhost:5070").replace(/\/$/, "");

const nextConfig: NextConfig = {
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${backend}/api/:path*` }];
  },
};

export default nextConfig;
