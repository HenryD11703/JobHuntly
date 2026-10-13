import type {
  AiStatus, ApplicationEvent, ApplicationStatus, DebriefDraft, DebriefInput, Insights, Interview,
  InterviewRound, InterviewStatus, JobApplication, JobPostDraft, NewApplicationInput, NewOfferInput,
  Nudge, Offer, OfferComparison, PrepResult, Profile, Rates, Story, StoryInput, SuggestedRates,
  UpdateApplicationInput,
} from "./types";

/** An error answered by the backend as RFC 7807 problem details. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    /** Stable machine-readable code such as "ai_quota_exceeded", when the backend sends one. */
    readonly code?: string,
    readonly resetsAt?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

interface Problem {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
  resetsAt?: string;
  errors?: Record<string, string[]>;
}

/** Turns a problem-details body into one short sentence a person can act on. */
export function describeProblem(status: number, problem: Problem | null): string {
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];
    if (first) return first;
  }
  if (problem?.detail) return problem.detail;
  if (status === 429) return "Too many requests. Please wait a moment and try again.";
  if (status >= 500) return "Something went wrong on our side. Please try again.";
  return problem?.title ?? "Something went wrong.";
}

async function request<T>(method: string, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  // The backend refuses state-changing requests that do not carry this header (CSRF guard).
  if (method !== "GET") headers["X-Requested-With"] = "jobhuntly";

  let response: Response;
  try {
    response = await fetch(path, {
      method,
      headers,
      credentials: "same-origin",
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ApiError(0, "Cannot reach the server. Check your connection and try again.", "network");
  }

  if (response.status === 204) return undefined as T;

  const text = await response.text();
  let json: unknown = null;
  if (text) {
    try {
      json = JSON.parse(text);
    } catch {
      json = null;
    }
  }

  if (!response.ok) {
    const problem = json as Problem | null;
    throw new ApiError(response.status, describeProblem(response.status, problem), problem?.code, problem?.resetsAt);
  }
  return json as T;
}

const get = <T>(path: string, signal?: AbortSignal) => request<T>("GET", path, undefined, signal);
const post = <T>(path: string, body?: unknown) => request<T>("POST", path, body ?? {});
const put = <T>(path: string, body: unknown) => request<T>("PUT", path, body);
const del = (path: string) => request<void>("DELETE", path);

const app = (id: string) => `/api/jobapplications/${id}`;
const iv = (id: string, interviewId: string) => `${app(id)}/interviews/${interviewId}`;

export const api = {
  auth: {
    me: (signal?: AbortSignal) => get<Profile>("/api/auth/me", signal),
    login: (email: string, password: string) => post<unknown>("/api/auth/login", { email, password }),
    register: (input: { email: string; password: string; firstName: string; lastName: string }) =>
      post<unknown>("/api/auth/register", input),
    demo: () => post<unknown>("/api/auth/demo"),
    logout: () => post<void>("/api/auth/logout"),
  },
  applications: {
    list: () => get<JobApplication[]>("/api/jobapplications"),
    get: (id: string) => get<JobApplication>(app(id)),
    create: (input: NewApplicationInput) => post<JobApplication>("/api/jobapplications", input),
    update: (id: string, input: UpdateApplicationInput) => put<JobApplication>(app(id), input),
    setStatus: (id: string, status: ApplicationStatus, rejectionReason?: string | null) =>
      put<JobApplication>(`${app(id)}/status`, { status, rejectionReason: rejectionReason ?? null }),
    remove: (id: string) => del(app(id)),
    timeline: (id: string) => get<ApplicationEvent[]>(`${app(id)}/timeline`),
    followUp: (id: string, note?: string) => post<ApplicationEvent>(`${app(id)}/follow-ups`, { note: note || null }),
  },
  interviews: {
    add: (id: string, interviewDate: string, roundType: InterviewRound, notes?: string) =>
      post<Interview>(`${app(id)}/interviews`, { interviewDate, roundType, notes: notes || null }),
    update: (id: string, interviewId: string, input: { interviewDate?: string; status?: InterviewStatus; notes?: string }) =>
      put<Interview>(iv(id, interviewId), input),
    tick: (id: string, interviewId: string, itemId: string, done: boolean) =>
      put<Interview>(`${iv(id, interviewId)}/checklist`, { itemId, done }),
    saveDebrief: (id: string, interviewId: string, input: DebriefInput) =>
      put<Interview>(`${iv(id, interviewId)}/debrief`, input),
  },
  ai: {
    status: () => get<AiStatus>("/api/ai/status"),
    extractJobPost: (text: string) => post<JobPostDraft>("/api/ai/extract-job-post", { text }),
    prep: (id: string, interviewId: string, regenerate = false) =>
      post<PrepResult>(`${iv(id, interviewId)}/prep`, { regenerate }),
    structureDebrief: (id: string, interviewId: string, rawNotes: string) =>
      post<DebriefDraft>(`${iv(id, interviewId)}/debrief/structure`, { rawNotes }),
  },
  today: () => get<Nudge[]>("/api/today"),
  insights: () => get<Insights>("/api/analytics/insights"),
  offers: {
    compare: () => get<OfferComparison>("/api/offers"),
    add: (applicationId: string, input: NewOfferInput) => post<Offer>(`${app(applicationId)}/offers`, input),
    remove: (id: string) => del(`/api/offers/${id}`),
  },
  rates: {
    get: () => get<Rates>("/api/settings/rates"),
    save: (rates: Rates) => put<Rates>("/api/settings/rates", rates),
    suggested: () => get<SuggestedRates>("/api/settings/rates/suggested"),
  },
  stories: {
    list: () => get<Story[]>("/api/stories"),
    create: (input: StoryInput) => post<Story>("/api/stories", input),
    update: (id: string, input: StoryInput) => put<Story>(`/api/stories/${id}`, input),
    remove: (id: string) => del(`/api/stories/${id}`),
  },
};
