"use client";

import { useState, type FormEvent } from "react";
import { Dialog } from "@/components/Dialog";
import { Empty, ErrorNotice, PageHead, useAction } from "@/components/ui";
import { api } from "@/lib/api";
import type { Story, StoryInput } from "@/lib/types";
import { useResource } from "@/lib/use-resource";

export default function StoriesPage() {
  const stories = useResource("stories", api.stories.list);
  const [editing, setEditing] = useState<Story | "new" | null>(null);
  const [deleting, setDeleting] = useState<Story | null>(null);
  const remove = useAction();
  const list = stories.data ?? [];

  async function confirmDelete(story: Story) {
    setDeleting(null);
    const done = await remove.run(async () => {
      await api.stories.remove(story.id);
      return true;
    });
    if (done) stories.reload();
  }

  return (
    <div className="wrap">
      <PageHead eyebrow={`${list.length} STAR stor${list.length === 1 ? "y" : "ies"}`} title="Stories">
        <button type="button" className="btn primary" onClick={() => setEditing("new")}>+ New story</button>
      </PageHead>
      <ErrorNotice error={stories.error ?? remove.error} />

      <section className="panel" aria-labelledby="bank-h">
        <h2 id="bank-h">Your story bank <span className="note">Situation · Task · Action · Result</span></h2>
        {stories.data && list.length === 0 && (
          <Empty title="No stories yet">
            Write 3 to 5 real examples (a bug you fixed, a conflict you solved, something you shipped). When you ask AI for
            likely questions, it matches them to these stories.
          </Empty>
        )}
        {list.map((story) => (
          <article className="story" key={story.id} aria-labelledby={`story-${story.id}`}>
            <div style={{ display: "flex", justifyContent: "space-between", gap: 12, flexWrap: "wrap", alignItems: "baseline" }}>
              <h3 id={`story-${story.id}`} style={{ fontSize: 24 }}>{story.title}</h3>
              <div className="actions">
                <button type="button" className="btn small" onClick={() => setEditing(story)} aria-label={`Edit ${story.title}`}>Edit</button>
                <button type="button" className="btn small danger" onClick={() => setDeleting(story)} aria-label={`Delete ${story.title}`}>Delete</button>
              </div>
            </div>
            {story.tags.length > 0 && <p style={{ marginTop: 6 }}>{story.tags.map((t) => <span key={t} className="tag" style={{ marginRight: 6 }}>{t}</span>)}</p>}
            <dl>
              <dt>Situation</dt><dd>{story.situation}</dd>
              <dt>Task</dt><dd>{story.task}</dd>
              <dt>Action</dt><dd>{story.action}</dd>
              <dt>Result</dt><dd>{story.result}</dd>
            </dl>
          </article>
        ))}
      </section>

      <Dialog open={editing !== null} title={editing === "new" ? "New story" : "Edit story"} onClose={() => setEditing(null)}>
        {editing !== null && (
          <StoryForm
            story={editing === "new" ? null : editing}
            onClose={() => setEditing(null)}
            onSaved={() => stories.reload()}
          />
        )}
      </Dialog>

      <Dialog open={deleting !== null} title="Delete story?" onClose={() => setDeleting(null)}>
        {deleting && (
          <>
            <p>Delete <b>{deleting.title}</b>? Saved interview questions that mention it keep their text.</p>
            <div className="dialog-actions">
              <button type="button" className="btn" onClick={() => setDeleting(null)}>Keep it</button>
              <button type="button" className="btn danger" onClick={() => void confirmDelete(deleting)}>Delete story</button>
            </div>
          </>
        )}
      </Dialog>
    </div>
  );
}

function StoryForm({ story, onClose, onSaved }: { story: Story | null; onClose: () => void; onSaved: () => void }) {
  const save = useAction();
  const [title, setTitle] = useState(story?.title ?? "");
  const [situation, setSituation] = useState(story?.situation ?? "");
  const [task, setTask] = useState(story?.task ?? "");
  const [action, setAction] = useState(story?.action ?? "");
  const [result, setResult] = useState(story?.result ?? "");
  const [tags, setTags] = useState((story?.tags ?? []).join(", "));

  async function submit(event: FormEvent) {
    event.preventDefault();
    const input: StoryInput = {
      title, situation, task, action, result,
      tags: tags.split(",").map((t) => t.trim()).filter(Boolean),
    };
    const saved = await save.run(() => (story ? api.stories.update(story.id, input) : api.stories.create(input)));
    if (saved) {
      onSaved();
      onClose();
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Story">
      <label><span>Title</span><input type="text" value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={150} /></label>
      <label><span>Situation: what was going on?</span><textarea value={situation} onChange={(e) => setSituation(e.target.value)} required maxLength={3000} rows={3} /></label>
      <label><span>Task: what was yours to do?</span><textarea value={task} onChange={(e) => setTask(e.target.value)} required maxLength={3000} rows={3} /></label>
      <label><span>Action: what did you do?</span><textarea value={action} onChange={(e) => setAction(e.target.value)} required maxLength={3000} rows={3} /></label>
      <label><span>Result: what changed? Use numbers if you can.</span><textarea value={result} onChange={(e) => setResult(e.target.value)} required maxLength={3000} rows={3} /></label>
      <label>
        <span>Tags (comma separated, up to 10)</span>
        <input type="text" value={tags} onChange={(e) => setTags(e.target.value)} placeholder="backend, incident, leadership" />
      </label>
      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Saving…" : "Save story"}</button>
      </div>
    </form>
  );
}
