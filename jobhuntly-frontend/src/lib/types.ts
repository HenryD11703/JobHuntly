// Types mirror the backend DTOs (JobHuntlyBackend/JobHuntly.Application/DTOs). Enums travel as strings.

export const STATUSES = ["Backlog", "Applied", "Interviewing", "TechnicalTest", "Offer", "Rejected"] as const;
export type ApplicationStatus = (typeof STATUSES)[number];

export const SOURCES = ["LinkedIn", "Referral", "CompanySite", "Recruiter", "Other"] as const;
export type ApplicationSource = (typeof SOURCES)[number];

export const ROUNDS = ["HrScreen", "Technical", "Behavioral", "TakeHome", "Final", "Other"] as const;
export type InterviewRound = (typeof ROUNDS)[number];

export type InterviewStatus = "Scheduled" | "Done" | "Cancelled";
export const CURRENCIES = ["USD", "GTQ", "EUR"] as const;
export type Currency = (typeof CURRENCIES)[number];
export type PayPeriod = "Monthly" | "Annual";
export type NudgeType = "Prepare" | "WriteDebrief" | "FollowUp";

export type EventType = "Created" | "StatusChanged" | "InterviewAdded" | "FollowedUp" | "DebriefSaved" | "OfferAdded";

export interface Profile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isDemo: boolean;
  demoExpiresAt: string | null;
}

export interface ChecklistItem {
  id: string;
  text: string;
  done: boolean;
}

export interface Debrief {
  rating: number;
  rawNotes: string;
  questionsAsked: string[];
  wentWell: string[];
  wentPoorly: string[];
  followUps: string[];
  savedAt: string;
}

export interface PrepQuestion {
  question: string;
  storyId: string | null;
  storyTitle: string | null;
}

export interface PrepQuestions {
  generatedAt: string;
  questions: PrepQuestion[];
}

export interface Interview {
  id: string;
  jobApplicationId: string;
  interviewDate: string;
  roundType: InterviewRound;
  status: InterviewStatus;
  notes: string | null;
  checklist: ChecklistItem[];
  checklistComplete: boolean;
  debrief: Debrief | null;
  prepQuestions: PrepQuestions | null;
}

export interface JobApplication {
  id: string;
  companyName: string;
  positionTitle: string;
  status: ApplicationStatus;
  appliedDate: string;
  notes: string | null;
  salaryRange: string | null;
  jobPostUrl: string | null;
  jobPostText: string | null;
  rejectionReason: string | null;
  source: ApplicationSource | null;
  location: string | null;
  isRemote: boolean;
  interviews: Interview[];
}

export interface ApplicationEvent {
  id: string;
  type: EventType;
  occurredAt: string;
  fromStatus: ApplicationStatus | null;
  toStatus: ApplicationStatus | null;
  note: string | null;
}

export interface Nudge {
  type: NudgeType;
  applicationId: string;
  interviewId: string | null;
  companyName: string;
  positionTitle: string;
  reason: string;
  since: string;
}

export interface FunnelStage {
  stage: string;
  count: number;
  percent: number | null;
}

export interface SourceReply {
  source: string;
  applications: number;
  replies: number;
  enoughData: boolean;
  replyRatePercent: number | null;
}

export interface StageDuration {
  stage: string;
  averageDays: number | null;
  samples: number;
}

export interface Insights {
  minGroupSize: number;
  totalApplications: number;
  appliedApplications: number;
  interviewRatePercent: number | null;
  averageDaysToFirstReply: number | null;
  funnel: FunnelStage[];
  sources: SourceReply[];
  stageDurations: StageDuration[];
  rejectionReasons: { reason: string; count: number }[];
}

export interface Offer {
  id: string;
  jobApplicationId: string;
  companyName: string;
  positionTitle: string;
  amount: number;
  currency: Currency;
  period: PayPeriod;
  notes: string | null;
  monthlyUsd: number;
  monthlyGtq: number;
}

export interface Rates {
  usdToGtq: number;
  eurToGtq: number;
}

export interface OfferComparison {
  rates: Rates;
  offers: Offer[];
}

export interface SuggestedRates extends Rates {
  providerUpdatedAt: string;
  provider: string;
  attributionUrl: string;
}

export interface Story {
  id: string;
  title: string;
  situation: string;
  task: string;
  action: string;
  result: string;
  tags: string[];
  createdAt: string;
}

export type StoryInput = Omit<Story, "id" | "createdAt">;

export interface AiStatus {
  enabled: boolean;
  dailyLimit: number;
  usedToday: number;
  resetsAt: string;
  provider: string;
}

export interface JobPostDraft {
  companyName: string | null;
  positionTitle: string | null;
  salaryRange: string | null;
  location: string | null;
  isRemote: boolean;
  requirements: string[];
}

export interface DebriefDraft {
  questionsAsked: string[];
  wentWell: string[];
  wentPoorly: string[];
  followUps: string[];
}

export interface PrepResult {
  prep: PrepQuestions;
  fromCache: boolean;
}

export interface NewApplicationInput {
  companyName: string;
  positionTitle: string;
  status?: ApplicationStatus;
  source?: ApplicationSource | null;
  notes?: string | null;
  salaryRange?: string | null;
  jobPostUrl?: string | null;
  jobPostText?: string | null;
  location?: string | null;
  isRemote?: boolean;
}

export interface UpdateApplicationInput {
  notes: string | null;
  salaryRange: string | null;
  jobPostUrl: string | null;
  jobPostText: string | null;
  rejectionReason: string | null;
  source: ApplicationSource | null;
  location: string | null;
  isRemote: boolean;
}

export interface DebriefInput {
  rating: number;
  rawNotes: string;
  questionsAsked: string[];
  wentWell: string[];
  wentPoorly: string[];
  followUps: string[];
}

export interface NewOfferInput {
  amount: number;
  currency: Currency;
  period: PayPeriod;
  notes?: string | null;
}
