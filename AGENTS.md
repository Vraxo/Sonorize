# Agent Instructions

## Pull requests

- Open a pull request for each focused, reviewable change; do not push unrelated work into the same PR.
- Every pull request must have GitHub auto-merge explicitly enabled immediately after it is created. This is mandatory for documentation, maintenance, refactoring, and feature changes alike.
- Confirm that enabling auto-merge succeeded. If GitHub rejects it, investigate and report the exact blocker; do not silently leave the PR waiting for manual merging.
- Keep required status checks and review requirements intact. Auto-merge must wait for all configured requirements; never bypass failing, pending, or required checks or reviews.
- Do not manually merge a PR as a workaround unless the user explicitly asks for that PR to be merged manually.
- Before opening a PR, run the relevant tests and build checks that are available, and summarize exactly what was and was not run in the PR description.
- Keep the user informed about each PR's scope, verification status, auto-merge status, and merge status. Do not silently proceed to unrelated work while awaiting confirmation.

## CI wait-and-check procedure

When a change triggers GitHub Actions or another asynchronous CI workflow, allow the checks time to settle before polling. Do this while the task is still actively running; do not return control to the user merely to wait for CI and ask them to prompt a later status check.

- After pushing the final commit for a PR or PR update, wait **90 seconds** before the first CI status check.
- If the relevant workflow is still queued or in progress, wait another **30–60 seconds** before checking again. Repeat as needed while actively working.
- Once a run completes, inspect its conclusion and failed job/step logs. A failed run requires diagnosis and a corrective change; after pushing that fix, restart the wait-and-check cycle.
- Verify the latest commit's required checks, not an earlier commit's run. Do not treat a stale green run as validation for newer code.
- Do not report a PR as merged until GitHub confirms it is merged. Auto-merge must remain enabled and must wait for all configured checks and review requirements.
- If the execution environment cannot sustain the wait or polling, state the limitation explicitly rather than implying that CI was checked.

This procedure applies to every agent and every PR, including documentation-only changes.
