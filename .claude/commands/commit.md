Create a well-formatted conventional commit.

Instructions:
1. Run `git status` to see all changes
2. Run `git diff --staged` to review staged changes (if any)
3. Run `git diff` to see unstaged changes
4. Run `git log -5 --oneline` to see recent commit style
5. Analyze changes and determine:
   - Type: feat, fix, docs, style, refactor, test, chore, build, ci
   - Scope: component affected (e.g., media, export, timeline)
   - Description: concise summary of the change
6. Stage appropriate files with `git add`
7. Create commit with format: `<type>(<scope>): <description>`

Commit message conventions for this project:
- feat: New feature or capability
- fix: Bug fix
- docs: Documentation only
- refactor: Code restructuring without behavior change
- test: Adding or updating tests
- build: Build system or dependency changes
- chore: Maintenance tasks
