Prepare a new release version.

Instructions:
1. Determine version bump type from user argument (major, minor, patch)
2. Read current version from src/gui/VapourSynthPortable/VapourSynthPortable.csproj
3. Calculate new version number following semver
4. Update `<Version>` element in .csproj file
5. Generate changelog from commits since last tag:
   - Windows: `git log --oneline HEAD` (if no tags exist)
   - With tags: `git describe --tags --abbrev=0` then `git log <tag>..HEAD --oneline`
6. Create commit: `chore(release): bump version to X.Y.Z`
7. Create tag: `git tag vX.Y.Z`
8. Report next steps (push, create GitHub release)

If user specifies `--dry-run`, show what would happen without making changes.

Version bump rules:
- major: Breaking changes (1.0.0 -> 2.0.0)
- minor: New features, backwards compatible (1.0.0 -> 1.1.0)
- patch: Bug fixes, backwards compatible (1.0.0 -> 1.0.1)
