"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";

interface Interview {
  id: string;
  interviewDate: string;
  roundType: string;
  notes: string;
}

interface JobApplication {
  id: string;
  companyName: string;
  positionTitle: string;
  status: number;
  notes?: string;
  salaryRange?: string;
  jobPostUrl?: string;
  rejectionReason?: string;
  appliedDate: string;
  interviews: Interview[];
}

interface Analytics {
  totalApplications: number;
  activeApplications: number;
  rejectedApplications: number;
  offersReceived: number;
  interviewConversionRate: number;
}

const COLUMNS = [
  { id: 0, title: "Backlog" },
  { id: 1, title: "Applied" },
  { id: 2, title: "Interviewing" },
  { id: 3, title: "Technical Test" },
  { id: 4, title: "Offer" },
  { id: 5, title: "Rejected" },
];

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5030";

export default function Board() {
  const [viewMode, setViewMode] = useState<"kanban" | "calendar">("kanban");
  const [cards, setCards] = useState<JobApplication[]>([]);
  const [analytics, setAnalytics] = useState<Analytics | null>(null);
  
  // Modals state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedCard, setSelectedCard] = useState<JobApplication | null>(null);

  // Form state
  const [newCompany, setNewCompany] = useState("");
  const [newRole, setNewRole] = useState("");
  
  // Edit form state
  const [editNotes, setEditNotes] = useState("");
  const [editSalary, setEditSalary] = useState("");
  const [editUrl, setEditUrl] = useState("");
  const [editReason, setEditReason] = useState("");

  // Interview state
  const [newInterviewDate, setNewInterviewDate] = useState("");
  const [newInterviewRound, setNewInterviewRound] = useState("");
  const [newInterviewNotes, setNewInterviewNotes] = useState("");

  const router = useRouter();

  const loadData = async () => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.push("/");
      return;
    }

    try {
      const [boardRes, analyticsRes] = await Promise.all([
        fetch(`${API_URL}/api/JobApplications`, { headers: { "Authorization": `Bearer ${token}` } }),
        fetch(`${API_URL}/api/Analytics/summary`, { headers: { "Authorization": `Bearer ${token}` } })
      ]);

      if (!boardRes.ok || !analyticsRes.ok) throw new Error("Unauthorized");
      
      setCards(await boardRes.json());
      setAnalytics(await analyticsRes.json());
    } catch {
      localStorage.removeItem("token");
      router.push("/");
    }
  };

  useEffect(() => {
    loadData();
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem("token");
    router.push("/");
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const token = localStorage.getItem("token");
    const res = await fetch(`${API_URL}/api/JobApplications`, {
      method: "POST",
      headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
      body: JSON.stringify({ companyName: newCompany, positionTitle: newRole, status: 0 })
    });

    if (res.ok) {
      setIsCreateModalOpen(false);
      setNewCompany("");
      setNewRole("");
      loadData(); // Reload to update analytics
    }
  };

  const updateStatus = async (id: string, newStatus: number) => {
    const token = localStorage.getItem("token");
    setCards(cards.map(c => c.id === id ? { ...c, status: newStatus } : c));

    await fetch(`${API_URL}/api/JobApplications/${id}/status`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
      body: JSON.stringify({ status: newStatus })
    });
    
    loadData(); // Reload analytics behind the scenes
  };

  const openCardDetails = (card: JobApplication) => {
    setSelectedCard(card);
    setEditNotes(card.notes || "");
    setEditSalary(card.salaryRange || "");
    setEditUrl(card.jobPostUrl || "");
    setEditReason(card.rejectionReason || "");
  };

  const handleUpdateDetails = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCard) return;

    const token = localStorage.getItem("token");
    await fetch(`${API_URL}/api/JobApplications/${selectedCard.id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
      body: JSON.stringify({ notes: editNotes, salaryRange: editSalary, jobPostUrl: editUrl, rejectionReason: editReason })
    });
    
    loadData();
    setSelectedCard(null);
  };

  const handleAddInterview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCard) return;

    const token = localStorage.getItem("token");
    await fetch(`${API_URL}/api/JobApplications/${selectedCard.id}/interviews`, {
      method: "POST",
      headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
      body: JSON.stringify({
        interviewDate: newInterviewDate,
        roundType: newInterviewRound,
        notes: newInterviewNotes
      })
    });

    setNewInterviewDate("");
    setNewInterviewRound("");
    setNewInterviewNotes("");
    
    setSelectedCard(prev => prev ? {
      ...prev,
      interviews: [...prev.interviews, {
        id: Math.random().toString(),
        interviewDate: newInterviewDate,
        roundType: newInterviewRound,
        notes: newInterviewNotes
      }]
    } : null);

    loadData();
  };

  // --- CALENDAR LOGIC ---
  const currentMonth = new Date().getMonth();
  const currentYear = new Date().getFullYear();
  const daysInMonth = new Date(currentYear, currentMonth + 1, 0).getDate();
  const firstDayOfMonth = new Date(currentYear, currentMonth, 1).getDay(); // 0 is Sunday
  
  const blanks = Array(firstDayOfMonth).fill(null);
  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1);
  const allSlots = [...blanks, ...days];

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      
      <nav style={{ padding: '24px 40px', borderBottom: '2px solid var(--color-border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', backgroundColor: 'var(--color-surface)' }}>
        <h2 style={{ fontSize: '1.5rem', color: 'var(--color-accent)' }}>JOBHUNTLY.</h2>
        <div style={{ display: 'flex', gap: '16px' }}>
          <button onClick={handleLogout} className="btn-outline">Logout</button>
        </div>
      </nav>

      {/* DASHBOARD ANALYTICS */}
      {analytics && (
        <div className="analytics-container">
          <div style={{ flex: 1, padding: '24px', backgroundColor: 'var(--color-text)', color: 'white', border: '2px solid var(--color-border-dark)' }}>
            <p style={{ fontSize: '12px', fontWeight: 600, color: 'var(--color-text-muted)' }}>TOTAL APPS</p>
            <h2 style={{ fontSize: '3rem' }}>{analytics.totalApplications}</h2>
          </div>
          <div style={{ flex: 1, padding: '24px', backgroundColor: 'var(--color-surface)', border: '2px solid var(--color-border-dark)' }}>
            <p style={{ fontSize: '12px', fontWeight: 600, color: 'var(--color-text-muted)' }}>CONVERSION RATE</p>
            <h2 style={{ fontSize: '3rem', color: 'var(--color-accent)' }}>{analytics.interviewConversionRate}%</h2>
          </div>
          <div style={{ flex: 1, padding: '24px', backgroundColor: 'var(--color-surface)', border: '2px solid var(--color-border-dark)' }}>
            <p style={{ fontSize: '12px', fontWeight: 600, color: 'var(--color-text-muted)' }}>OFFERS</p>
            <h2 style={{ fontSize: '3rem' }}>{analytics.offersReceived}</h2>
          </div>
        </div>
      )}

      <header style={{ padding: '40px', paddingBottom: '0', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        
        <div className="segmented-control">
          <button 
            className={`segmented-btn ${viewMode === 'kanban' ? 'active' : ''}`} 
            onClick={() => setViewMode('kanban')}
          >
            PIPELINE
          </button>
          <button 
            className={`segmented-btn ${viewMode === 'calendar' ? 'active' : ''}`} 
            onClick={() => setViewMode('calendar')}
          >
            CALENDAR
          </button>
        </div>

        <button onClick={() => setIsCreateModalOpen(true)} className="btn-primary">+ NEW APP</button>
      </header>

      {/* RENDER KANBAN OR CALENDAR */}
      {viewMode === "kanban" ? (
        <main style={{ flex: 1, padding: '40px', display: 'flex', gap: '24px', overflowX: 'auto', alignItems: 'flex-start' }}>
          {COLUMNS.map(col => (
            <div 
              key={col.id} 
              style={{ minWidth: '320px', backgroundColor: 'var(--color-surface)', border: '2px solid var(--color-border)', padding: '24px' }}
              onDragOver={(e) => e.preventDefault()}
              onDrop={(e) => {
                const cardId = e.dataTransfer.getData("cardId");
                if (cardId) updateStatus(cardId, col.id);
              }}
            >
              <h3 style={{ fontSize: '14px', color: 'var(--color-text-muted)', marginBottom: '24px', borderBottom: '2px solid var(--color-border)', paddingBottom: '12px' }}>
                {col.title.toUpperCase()}
              </h3>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', minHeight: '100px' }}>
                {cards.filter(c => c.status === col.id).map(card => (
                  <div 
                    key={card.id} 
                    draggable
                    onDragStart={(e) => e.dataTransfer.setData("cardId", card.id.toString())}
                    onClick={() => openCardDetails(card)}
                    style={{ padding: '20px', border: '2px solid var(--color-border-dark)', backgroundColor: '#fff', boxShadow: '4px 4px 0px var(--color-accent)', cursor: 'grab' }}
                  >
                    <h4 style={{ fontSize: '18px', marginBottom: '4px' }}>{card.companyName}</h4>
                    <p style={{ fontSize: '14px', color: 'var(--color-text-muted)', fontWeight: 600 }}>{card.positionTitle}</p>
                    
                    <div style={{ display: 'flex', gap: '8px', marginTop: '12px' }}>
                      {card.interviews.length > 0 && (
                        <div style={{ fontSize: '10px', padding: '4px 8px', backgroundColor: 'var(--color-accent)', color: 'white', fontWeight: 600 }}>
                          {card.interviews.length} INTERVIEWS
                        </div>
                      )}
                      {card.notes && (
                        <div style={{ fontSize: '10px', padding: '4px 8px', backgroundColor: 'var(--color-bg)', fontWeight: 600 }}>
                          NOTES
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </main>
      ) : (
        <main style={{ flex: 1, padding: '40px' }} className="calendar-container">
          <div className="calendar-grid">
            {/* Weekdays Header */}
            {['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'].map(day => (
              <div key={day} style={{ backgroundColor: 'var(--color-surface)', padding: '16px', fontWeight: 600, fontSize: '12px', textAlign: 'center', color: 'var(--color-text-muted)' }}>
                {day}
              </div>
            ))}
            
            {/* Calendar Cells */}
            {allSlots.map((day, i) => {
              // Find events for this day
              const dayEvents = day ? cards.flatMap(card => {
                const events = [];
                // Did they apply on this day?
                const appliedDate = new Date(card.appliedDate);
                if (appliedDate.getDate() === day && appliedDate.getMonth() === currentMonth && appliedDate.getFullYear() === currentYear) {
                  events.push({ type: 'applied', text: `Applied: ${card.companyName}`, card });
                }
                // Do they have interviews on this day?
                card.interviews.forEach(inv => {
                  const invDate = new Date(inv.interviewDate);
                  if (invDate.getDate() === day && invDate.getMonth() === currentMonth && invDate.getFullYear() === currentYear) {
                    events.push({ type: 'interview', text: `Interview: ${card.companyName}`, time: inv.roundType, card });
                  }
                });
                return events;
              }) : [];

              return (
                <div key={i} className="calendar-cell">
                  {day && (
                    <>
                      <div style={{ width: '100%', textAlign: 'right', fontSize: '14px', fontWeight: 600, marginBottom: '8px', color: 'var(--color-text-muted)' }}>{day}</div>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                        {dayEvents.map((evt, idx) => (
                          <div 
                            key={idx} 
                            className="calendar-event"
                            onClick={() => openCardDetails(evt.card)}
                            style={{ 
                              backgroundColor: evt.type === 'interview' ? 'var(--color-accent)' : 'var(--color-bg)', 
                              color: evt.type === 'interview' ? '#fff' : 'var(--color-text)',
                              border: evt.type === 'interview' ? 'none' : '1px solid var(--color-border)',
                            }}
                          >
                            {evt.text}
                          </div>
                        ))}
                      </div>
                    </>
                  )}
                </div>
              );
            })}
          </div>
        </main>
      )}

      {/* CREATE MODAL */}
      {isCreateModalOpen && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 100 }}>
          <div className="modal-content-small">
            <h2 style={{ marginBottom: '24px' }}>NEW APPLICATION</h2>
            <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div><label>Company</label><input required value={newCompany} onChange={e => setNewCompany(e.target.value)} /></div>
              <div><label>Role</label><input required value={newRole} onChange={e => setNewRole(e.target.value)} /></div>
              <div style={{ display: 'flex', gap: '12px', marginTop: '16px' }}>
                <button type="submit" className="btn-primary" style={{ flex: 1 }}>Save</button>
                <button type="button" className="btn-outline" onClick={() => setIsCreateModalOpen(false)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* DETAILS / EDIT MODAL */}
      {selectedCard && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 100 }}>
          <div className="modal-content">
            
            {/* Left Col: Details */}
            <div style={{ flex: 1 }}>
              <h2 style={{ marginBottom: '8px' }}>{selectedCard.companyName}</h2>
              <p style={{ color: 'var(--color-text-muted)', fontWeight: 600, marginBottom: '32px' }}>{selectedCard.positionTitle}</p>

              <div style={{ marginBottom: '24px' }}>
                <label>Current Status</label>
                <select 
                  value={selectedCard.status}
                  onChange={(e) => {
                    const newStatus = parseInt(e.target.value);
                    updateStatus(selectedCard.id, newStatus);
                    setSelectedCard(prev => prev ? { ...prev, status: newStatus } : null);
                  }}
                  style={{ backgroundColor: 'var(--color-bg)', fontWeight: 600 }}
                >
                  {COLUMNS.map(c => (
                    <option key={c.id} value={c.id}>{c.title.toUpperCase()}</option>
                  ))}
                </select>
                <small style={{ color: 'var(--color-text-muted)', fontSize: '12px', marginTop: '4px', display: 'block' }}>Change status here if you can't drag and drop on mobile.</small>
              </div>

              <form onSubmit={handleUpdateDetails} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                <div><label>Salary Range</label><input value={editSalary} onChange={e => setEditSalary(e.target.value)} /></div>
                <div><label>Job Post URL</label><input type="url" value={editUrl} onChange={e => setEditUrl(e.target.value)} /></div>
                <div><label>Application Notes</label><textarea rows={5} value={editNotes} onChange={e => setEditNotes(e.target.value)} /></div>
                
                {selectedCard.status === 5 && (
                  <div>
                    <label>Reason for Rejection</label>
                    <textarea rows={3} placeholder="What can be improved for next time?" value={editReason} onChange={e => setEditReason(e.target.value)} />
                  </div>
                )}

                <div style={{ display: 'flex', gap: '12px', marginTop: '16px' }}>
                  <button type="submit" className="btn-primary" style={{ flex: 1 }}>Save Details</button>
                  <button type="button" className="btn-outline" onClick={() => setSelectedCard(null)}>Close</button>
                </div>
              </form>
            </div>

            {/* Right Col: Interviews */}
            <div className="modal-column-right">
              <h3 style={{ marginBottom: '24px' }}>INTERVIEWS ({selectedCard.interviews.length})</h3>
              
              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginBottom: '32px' }}>
                {selectedCard.interviews.map(inv => (
                  <div key={inv.id} style={{ padding: '16px', backgroundColor: 'var(--color-bg)', border: '2px solid var(--color-border-dark)' }}>
                    <p style={{ fontSize: '12px', fontWeight: 600, color: 'var(--color-accent)' }}>{new Date(inv.interviewDate).toLocaleDateString()}</p>
                    <h4 style={{ marginBottom: '8px' }}>{inv.roundType}</h4>
                    {inv.notes && <p style={{ fontSize: '14px' }}>{inv.notes}</p>}
                  </div>
                ))}
              </div>

              <h4 style={{ marginBottom: '16px' }}>LOG NEW INTERVIEW</h4>
              <form onSubmit={handleAddInterview} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div><label>Date</label><input type="date" required value={newInterviewDate} onChange={e => setNewInterviewDate(e.target.value)} /></div>
                <div><label>Round Type</label><input placeholder="e.g. Technical" required value={newInterviewRound} onChange={e => setNewInterviewRound(e.target.value)} /></div>
                <div><label>Notes</label><textarea rows={2} value={newInterviewNotes} onChange={e => setNewInterviewNotes(e.target.value)} /></div>
                <button type="submit" className="btn-outline">Add Interview</button>
              </form>
            </div>

          </div>
        </div>
      )}

    </div>
  );
}
