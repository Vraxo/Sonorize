# Agent Instructions

## Pull requests

- Open a pull request for each focused, reviewable change; do not push unrelated work into the same PR.
- Enable GitHub auto-merge on every pull request rather than manually merging it.
- Auto-merge must wait for all required CI checks to pass. Never bypass failing, pending, or missing required checks just to get a PR merged.
- If the repository has no required checks configured, do not allow auto-merge to merge the PR immediately; report that CI protection is missing and leave the PR open until validation is available.
- Before requesting auto-merge, run the relevant tests and build checks that are available, and summarize their results in the PR description.
- Keep the user informed about each PR's scope, validation status, and merge status. Do not silently proceed to unrelated work while awaiting confirmation.
