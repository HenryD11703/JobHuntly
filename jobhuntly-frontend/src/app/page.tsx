"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

export default function Home() {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [error, setError] = useState("");
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    
    try {
      const endpoint = isLogin ? "/api/auth/login" : "/api/auth/register";
      const payload = isLogin 
        ? { email, password } 
        : { email, password, firstName, lastName };

      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5030";
      const res = await fetch(`${baseUrl}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      if (!res.ok) {
        const errorData = await res.json();
        throw new Error(errorData.message || "Something went wrong");
      }

      const data = await res.json();
      localStorage.setItem("token", data.token);
      router.push("/board");
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <div className="auth-container" style={{ display: 'flex', minHeight: '100vh', width: '100vw' }}>
      
      {/* Left side: branding (Swiss style) */}
      <div className="auth-left" style={{
        flex: 1, 
        backgroundColor: 'var(--color-accent)', 
        color: 'white',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        padding: '60px'
      }}>
        <h1 style={{ fontSize: '4rem', marginBottom: '20px' }}>
          JOB<br/>HUNTLY.
        </h1>
        <p style={{ fontSize: '1.2rem', fontWeight: 600, maxWidth: '400px' }}>
          THE ONLY PIPELINE YOU NEED TO SECURE YOUR NEXT ROLE. 
          TRACK YOUR APPLICATIONS WITH ABSOLUTE CLARITY.
        </p>
      </div>

      {/* Right side: form */}
      <div className="auth-right" style={{
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        padding: '60px',
        backgroundColor: 'var(--color-surface)'
      }}>
        <div style={{ maxWidth: '400px', width: '100%' }}>
          <h2 style={{ fontSize: '2rem', marginBottom: '40px', color: 'var(--color-text)' }}>
            {isLogin ? "LOG IN" : "REGISTER"}
          </h2>

          {error && (
            <div style={{ padding: '12px', backgroundColor: '#FFEBEB', color: '#D32F2F', border: '1px solid #D32F2F', marginBottom: '20px' }}>
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            {!isLogin && (
              <>
                <div>
                  <label htmlFor="firstName">First Name</label>
                  <input type="text" id="firstName" required value={firstName} onChange={(e) => setFirstName(e.target.value)} />
                </div>
                <div>
                  <label htmlFor="lastName">Last Name</label>
                  <input type="text" id="lastName" required value={lastName} onChange={(e) => setLastName(e.target.value)} />
                </div>
              </>
            )}

            <div>
              <label htmlFor="email">Email Address</label>
              <input 
                type="email" 
                id="email" 
                required 
                placeholder="hello@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            
            <div>
              <label htmlFor="password">Password</label>
              <input 
                type="password" 
                id="password" 
                required 
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>

            <button type="submit" className="btn-primary" style={{ marginTop: '16px' }}>
              {isLogin ? "Access Board" : "Create Account"}
            </button>
          </form>

          <div style={{ marginTop: '40px', textAlign: 'center' }}>
            <button 
              type="button"
              onClick={() => { setIsLogin(!isLogin); setError(""); }}
              style={{ color: 'var(--color-text-muted)', fontWeight: 600, fontSize: '12px', textTransform: 'uppercase' }}
            >
              {isLogin ? "No account? Register here" : "Already have an account? Log in"}
            </button>
          </div>
        </div>
      </div>

    </div>
  );
}
