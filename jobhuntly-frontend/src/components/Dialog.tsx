"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";

interface DialogProps {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
}

/**
 * A modal built on the native <dialog> element, so the browser provides the focus trap, the Escape
 * key and the inert background. Children are only rendered while open, so forms always start fresh.
 */
export function Dialog({ open, title, onClose, children }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    // React bubbles a dialog's close/cancel events up through the React tree, so closing a dialog nested
    // inside another one (the AI notice inside a form) would close the outer dialog too. Stop that here.
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onClose={(event) => { event.stopPropagation(); onClose(); }}
      onCancel={(event) => { event.stopPropagation(); onClose(); }}
    >
      {open && (
        <>
          <div className="dialog-head">
            <h2 id={titleId}>{title}</h2>
            <button type="button" className="btn small" onClick={onClose}>
              Close<span className="visually-hidden"> dialog</span>
            </button>
          </div>
          <div className="dialog-body">{children}</div>
        </>
      )}
    </dialog>
  );
}
