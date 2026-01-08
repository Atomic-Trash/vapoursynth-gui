Generate or update project documentation.

Subcommands (pass as argument):
- `architecture` - Analyze codebase and generate ARCHITECTURE.md
- `readme` - Update README.md with current features
- `changelog` - Generate CHANGELOG.md from git history

## For `/docs architecture`:
1. Scan src/gui/VapourSynthPortable/ for:
   - ViewModels in ViewModels/
   - Services in Services/
   - Pages in Pages/
   - Controls in Controls/
   - Models in Models/
2. Document the MVVM structure and service relationships
3. Create/update docs/ARCHITECTURE.md with:
   - Project overview
   - Component diagram (text-based)
   - Service dependencies
   - Data flow descriptions

## For `/docs readme`:
1. Read current README.md
2. Scan for new features in ViewModels and Pages
3. Update feature list and screenshots section
4. Ensure installation and usage instructions are current

## For `/docs changelog`:
1. Get commits since last tag:
   - `git describe --tags --abbrev=0` to find last tag
   - `git log <tag>..HEAD --oneline` to get commits
2. Group commits by type (feat, fix, docs, etc.)
3. Format as CHANGELOG.md entry with date
4. Prepend to existing CHANGELOG.md or create new one
