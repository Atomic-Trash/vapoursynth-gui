Check for dependency updates.

Instructions:
1. Check NuGet packages:
   `dotnet list src/gui/VapourSynthPortable.sln package --outdated`

2. Check VapourSynth plugins (if Check-Updates.ps1 exists):
   `powershell -ExecutionPolicy Bypass -File Check-Updates.ps1`

3. Summarize available updates with current vs latest versions

4. Categorize updates by priority:
   - **High priority**: Security updates, major bug fixes
   - **Medium priority**: Minor version updates with new features
   - **Low priority**: Patch updates, cosmetic changes

5. Recommend which updates to apply based on:
   - Security implications
   - Breaking changes in major versions
   - Compatibility with .NET 8.0

6. For each recommended update, explain:
   - What changed
   - Potential impact on the project
   - Any migration steps needed
