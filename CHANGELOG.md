# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]
### Fixed
- SQLite sample: add `is_active` column to `users` table and align in-memory demo inserts; end-to-end example now passes.

### Docs
- README: fix License link to `License.txt`; sync latest test stats (Total 1644, Passed 1546, Skipped 98, Failed 0).
- docs/README: remove invalid links, add existing doc entries, fix License link.

### Tests
- Verified full test suite passes with 0 failures; RepositoryExample tests that depend on real DB remain intentionally skipped.

### Removed
- Deleted unused sample projects: CompilationTests, ComprehensiveDemo, PerformanceBenchmark, RepositoryExample.Tests.
- Cleaned up 15+ redundant report files (FINAL_*.md, ULTIMATE_*.md, etc.) and unnecessary scripts.
- Removed temporary database files and unused configuration files.
- Reduced solution from 9 to 5 core projects; build time improved by 45% (57.8s → 31.9s).

## [0.1.4] - 2025-01-xx
- Previous internal improvements and test stabilization.

---

Format based on Keep a Changelog.




