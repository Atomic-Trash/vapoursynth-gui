Create a pull request with structured description.

Instructions:
1. Check current branch: `git branch --show-current`
2. Ensure changes are pushed: `git push -u origin HEAD`
3. Analyze commits: `git log main..HEAD --oneline`
4. Run `git diff main..HEAD --stat` for change summary
5. Create PR using gh CLI:

```bash
gh pr create --title "<title>" --body "$(cat <<'EOF'
## Summary
<brief description>

## Changes
<list of changes>

## Testing
- [ ] Build passes (`dotnet build`)
- [ ] Tests pass (`dotnet test`)
- [ ] Manual testing completed

## Screenshots
<if UI changes>
EOF
)"
```

If on main/master branch, remind user to create a feature branch first.
