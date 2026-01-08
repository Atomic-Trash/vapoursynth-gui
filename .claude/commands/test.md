Run the unit test suite.

Instructions:
1. Build first: `dotnet build src/gui/VapourSynthPortable.sln --no-restore`
2. Run tests: `dotnet test src/gui/VapourSynthPortable.sln --no-build --verbosity normal`
3. Summarize results: passed, failed, skipped counts
4. For failures, show test name and error message

If user specifies `--filter <pattern>`, add `--filter "<pattern>"` to test command.
