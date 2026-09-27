# Known issues

Most items below were found in the [viking-air](https://github.com/Digvijay/viking-air) integration
demo rather than by AutoMappic's own tests, because those exercise the generator inside a single
assembly, on one target framework, in one locale. Entries are kept after they are resolved so that
anyone evaluating a specific released version can see what applied to it.

| # | Issue | Severity | Status |
|---|---|---|---|
| 1 | Generated registration was `internal` | High | **Fixed** |
| 2 | Invalid code for hyphenated assembly names | High | **Fixed** |
| 3 | Interceptors lost for transitive consumers | High | **Fixed** |
| 4 | Test suite could not run on the .NET 10 SDK | Moderate | **Fixed** |
| 5 | Test suite was locale-dependent | Moderate | **Fixed** |
| 6 | `Profile.CreateMap` annotated as requiring dynamic code | High for AOT | **Fixed** |
| 7 | `PrivateAssets="all"` fails at run time, not build time | Moderate | **Withdrawn, did not reproduce** |
| 8 | Higher allocation than both alternatives | Moderate | **Fixed** |
| 9 | CLI test project was in no solution and had never run | High | **Fixed** |
| 10 | CLI and benchmarks targeted `net9.0`, not an LTS release | Moderate | **Fixed** |
| 11 | Two transitive packages carried high-severity advisories | High | **Fixed** |
| 12 | No benchmark could build, so the published results were unreproducible | High | **Fixed** |
| 13 | Benchmarks pinned to a runtime the project does not ship | Moderate | **Fixed** |
| 14 | Benchmarks never covered `net8.0`, which the package ships | Moderate | **Fixed** |
| 15 | `Regex.Matches(...).Count` allocated a match collection in the CLI | Low | **Fixed** |
| 16 | CI coverage step passed a switch the test platform rejects | High | **Fixed** |
| 17 | Version 0.7.0 would be rebuilt with different contents | High | **Fixed** |

---

# Fixed

## 1. Generated registration was `internal`, so cross-assembly use did not compile

**Severity: high. Blocked the most common project layout.**

The generator runs in every project that references an assembly containing mapping configuration
and emits a registration that calls the declaring assembly's `<Assembly>_Registration` class. That
class was `internal`, so every consuming project failed:

```
error CS0122: 'VikingAir_Core_Registration' is inaccessible due to its protection level
```

This blocked the ordinary layering where models and profiles live in a Core library that the API,
tests, and benchmarks reference. Neither `PrivateAssets="analyzers"` on the reference nor
`ExcludeAssets="analyzers;build"` downstream suppressed the generator in the consuming project, so
the only workaround was granting `InternalsVisibleTo` from the declaring assembly to every
consuming assembly — not a reasonable requirement to place on consumers.

**Fixed:** the registration class is emitted `public` and marked
`[EditorBrowsable(EditorBrowsableState.Never)]`, so it does not appear in IntelliSense.

## 2. Invalid code generated when the assembly name contained a hyphen

**Severity: high.**

This was originally reported as "the generator emits uncompilable code in BenchmarkDotNet host
projects". The real cause was considerably broader.

The generator derives C# identifiers — marker class, registration class, DI extension method — from
the compilation's assembly name. Sanitisation replaced only a hardcoded list of characters, and that
list did not include `-`. A hyphen is legal in an assembly name and illegal in a C# identifier, so a
project named `my-app` emitted:

```csharp
public static class AutoMappic_Extension_my-app
```

The compiler parsed that as a subtraction expression:

```
error CS0116: A namespace cannot directly contain members such as fields, methods or statements
error CS1106: Extension method must be defined in a non-generic static class
error CS0548: property or indexer must have at least one accessor
```

Any project whose assembly name contains a hyphen could not consume AutoMappic at all. Because
BenchmarkDotNet names its generated host assembly `<Project>-<Job>-<N>`, *every* BenchmarkDotNet
project that referenced AutoMappic failed to build, which is how the defect was found.

**Fixed:** sanitisation now guarantees a valid C# identifier for any input, including prefixing a
leading digit. `tests/AutoMappic.Tests/AssemblyNameSanitisationTests.cs` parses the generated
registration for `my-app`, `VikingAir.Benchmarks-DefaultJob-1`, `Contoso.Api-v2`, and `7Eleven.Api`
and requires it to be syntactically valid C#. BenchmarkDotNet projects no longer need the
`InProcessEmitToolchain` workaround and run with full process isolation.

## 3. Interceptors were silently disabled for transitive consumers

**Severity: high, because the failure is silent.**

AutoMappic's interception depends on an MSBuild property naming the generated namespace. Two
problems compounded:

* `AutoMappic.targets` was packed only to `build/`, not `buildTransitive/`. A project that picked
  AutoMappic up transitively never received the property, so interception silently did not apply
  and calls fell back to the reflection-based path — slower, allocating, and AOT-unsafe, with no
  diagnostic of any kind.
* The property name differs by language version. C# 12 / .NET 8 uses
  `InterceptorsPreviewNamespaces`; C# 13 / .NET 9+ uses `InterceptorsNamespaces` and raises
  **CS9137** if the preview property is also set. An earlier revision of this file suggested
  setting both — that is incorrect and breaks every .NET 9+ consumer.

**Fixed:** `AutoMappic.targets` selects the correct property conditionally on the target framework
version and is packed to both `build/` and `buildTransitive/`.

For a library whose entire value proposition is measured in nanoseconds and bytes, silently falling
back to the slow path is the worst possible failure mode: everything still works, and the only
symptom is that the benefit is absent.

## 4. The test suite could not run on the .NET 10 SDK

**Severity: moderate.**

`dotnet test` on the .NET 10 SDK refuses the legacy VSTest bridge for Microsoft.Testing.Platform
projects:

```
Testing with VSTest target is no longer supported by Microsoft.Testing.Platform
```

The suite did not run at all. It reported no failures because it executed no tests.

**Fixed** by opting in through `global.json`:

```json
{ "test": { "runner": "Microsoft.Testing.Platform" } }
```

Note that this is the correct mechanism — neither `dotnet.config` nor the
`TestingPlatformDotnetTestSupport` MSBuild property (the .NET 9-and-earlier opt-in) works here.

## 5. The test suite was locale-dependent

**Severity: moderate.**

Once the tests ran again, one failed immediately on a Swedish machine: a test value converter
formatted currency with the ambient culture, so `$99.99` became `$99,99`. The suite would have
failed for any contributor outside a culture using `.` as the decimal separator.

**Fixed** by formatting with `InvariantCulture`.

---

## 6. `Profile.CreateMap` was annotated as requiring dynamic code

**Severity: high for AOT consumers. Fixed in part; the remainder is deliberate.**

Publishing with `PublishAot=true` produced IL2026 and IL3050 warnings from AutoMappic's own public
API, which directly undercut the project's headline claim: the most discoverable API was the one
marked as needing reflection.

`Profile.CreateMap<TSource, TDestination>()` carried `[RequiresDynamicCode]`, and that annotation
was simply wrong. The method body constructs a `MappingExpression<TSource, TDestination>` and adds
it to a list; that type's constructor is a single field assignment, and its own documentation
records that the generator reads `CreateMap` calls from the syntax tree rather than instantiating
the class. No dynamic code is involved on that path. The attribute has been removed.

`[RequiresUnreferencedCode]` on the same method is **kept deliberately**. It describes trimming,
not AOT, and the reflection-based fallback in `Mapper` does call `GetProperties()` on both types.

The annotations on `IMapper.Map<T>` are also **kept deliberately**, and the earlier plan to move
them onto the concrete `Mapper` was abandoned after testing: the trimming analyzer requires an
interface and its implementation to carry matching annotations, and mismatches produce IL2046 and
IL3003. More importantly the annotation is honest. Interception is best-effort — a call site where
the types are not statically known falls back to reflection and would genuinely fail under AOT.

What was actually wrong was the documentation, not the annotation. The AOT-safe API is the
generated static extension method:

```csharp
var dto = person.MapToPersonDto();   // AOT-safe, no annotation, no fallback
```

`AotAnnotationTests` now pins all of this, so the decision cannot be quietly reversed.

**Methodology note, recorded because it nearly produced a false all-clear.** Three separate
attempts to reproduce this found no warnings at all. `IsAotCompatible=true` is a library-only
switch, and even `PublishAot=true` on its own reported nothing — including for a deliberate
`Type.MakeGenericType` control probe added specifically to test whether the analyzer was running.
Warnings appeared only after setting `EnableAotAnalyzer`, `EnableTrimAnalyzer` and
`EnableSingleFileAnalyzer` *and* re-running `dotnet restore`, because `PublishAot` changes the
restore graph. An AOT clean bill of health that was not accompanied by a control probe should not
be believed.

## 7. `PrivateAssets="all"` producing a runtime failure — withdrawn

**This entry did not reproduce and is retained only so the record is honest.**

The claim was that `PrivateAssets="all"` on the `AutoMappic` package reference withholds
`AutoMappic.Core.dll` and produces a `FileNotFoundException` on the first map. Tested against a
real consumer built from a locally packed `AutoMappic.0.7.0` package, it does not:
`AutoMappic.Core.dll` is copied to the output directory, it appears in `deps.json`, the
interceptor is emitted, and the application runs correctly.

`PrivateAssets` governs flow to *downstream* projects rather than to the referencing project
itself, and the package ships its targets under both `build/` and `buildTransitive/`, so the
import is not lost either.

**Scope of the test, stated plainly:** an application was verified. A library that itself packs
and expects consumers to inherit the generator was not, and that narrower case may still be real.
It is untested rather than disproven.

## 8. Higher allocation than both alternatives

**Severity: moderate. Fixed — it was a boxing defect, not a design trade-off.**

The generated mapping body opened with an unconditional box of the identity key:

```csharp
var __keyVal = (object?)source.Id;          // boxes an int on every single map
if (__keyVal != null) { context.TryGetEntity<...>(__keyVal, out var existing); ... }
```

`MappingContext` is a `readonly struct` holding a nullable dictionary, and the interceptor
constructs it with tracking disabled, so `TryGetEntity` and `Register` were both no-ops. The
allocation was pure waste on every call. The emitter also guarded the surrounding block with
`if (true && keyProp != null)`, so the entity-sync feature flag was hardwired on and the dead
branch was never eliminated.

The key is now materialised only when the context is actually tracking:

```csharp
var __keyVal = context.IsTracking ? (object?)source.Id : null;
```

With that removed, the generated body is byte-for-byte equivalent to the hand-written mapper.
Measured on a Snapdragon X Elite X1E80100 (ARM64), BenchmarkDotNet 0.15.8, 30 iterations:

**.NET 10**

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| Hand-written | 7.82 ns | 48 B |
| **AutoMappic** | **7.51 ns** | **48 B** |
| Mapperly | 7.65 ns | 48 B |
| AutoMapper | 57.27 ns | 48 B |

**.NET 8**

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| Hand-written | 10.33 ns | 48 B |
| **AutoMappic** | **11.63 ns** | **48 B** |
| Mapperly | 10.77 ns | 48 B |
| AutoMapper | 60.20 ns | 48 B |

Allocation is now identical to hand-written on both frameworks. AutoMappic is marginally faster
than hand-written on .NET 10 and about 12% slower on .NET 8 — a difference that only became
visible once the benchmarks covered both shipped frameworks, and which is the reason item 14
matters.

**On the figures this entry used to quote.** The previous table claimed 136 B for hand-written
against 208 B for AutoMappic at 56 ns and 68 ns. None of those numbers can be reproduced, and
item 12 explains why: no benchmark in this repository could build at all. The old table did not
come from this source tree. Boxing accounted for part of a gap that turned out not to exist as
stated, and the correct conclusion is that the measurement was never trustworthy in the first
place.

## 9. The CLI test project was in no solution and had never run

**Severity: high. Fixed.**

`tests/AutoMappic.Cli.Tests` existed on disk, compiled, and contained tests, but was listed in
neither `AutoMappic.sln` nor any build script. Nothing referenced it, so CI never restored it,
never built it and never executed it. The project had, in effect, never run at any point in its
history.

Adding it to the solution immediately exposed item 10 below: it failed to restore. A test that is
not in the build graph is not a test; it is a file that resembles one.

**Fix:** the project is listed in `AutoMappic.sln`, so `dotnet test` on the solution runs it. Its
four tests now execute on every CI run.

---

## 10. The CLI and benchmarks targeted `net9.0`, which is not an LTS release

**Severity: moderate. Fixed.**

`AutoMappic.Cli`, `AutoMappic.Benchmarks`, `SampleApp` and `AotBenchmark` each pinned
`<TargetFramework>net9.0</TargetFramework>`. `net9.0` is an STS release whose support window closes
well before the LTS the libraries ship against, so the tool shipped on a framework that would fall
out of support while the library it drives was still supported.

It also made the CLI untestable alongside the rest of the suite: adding the CLI test project
produced `NU1201 Project AutoMappic.Cli is not compatible with net8.0`.

**Fix:** all four projects now use `$(ToolkitAppTargetFrameworks)` — the current release, plus the
preview framework when the opt-in switch is set. The framework choice is made in one place for the
whole repository rather than restated per project.

---

## 11. Two transitive packages carried high-severity advisories

**Severity: high. Fixed.**

Restore reported `NU1903` for two packages that no project referenced directly:

| Package | Version | Reached via | Advisories |
| :--- | :--- | :--- | :--- |
| `Microsoft.OpenApi` | 2.0.0 | `Microsoft.AspNetCore.OpenApi` 10.0.1 | GHSA-v5pm-xwqc-g5wc |
| `System.Security.Cryptography.Xml` | 9.0.0 | `Microsoft.CodeAnalysis.Workspaces.MSBuild` | eight, all high severity |

The second only became visible after item 10 was fixed, because the CLI had not previously been
restored as part of a build whose warnings anyone saw.

**Fix:** both are pinned to patched versions in `Directory.Packages.props`, each with a comment
recording why the pin exists so that the next person does not delete what looks like a redundant
transitive reference. Restore is now free of `NU1902` and `NU1903`.

---

## 12. No benchmark could build, so the published results were unreproducible

**Severity: high. The project''s central claim is a performance claim.**

Every benchmark in this repository failed before it measured anything:

```
error MSB3030: Could not copy the file "...\AutoMappic.Generator.CodeFixes.Version.cs.new"
               because it was not found.
// BenchmarkDotNet has failed to build the auto-generated boilerplate code.
```

BenchmarkDotNet generates a host project and rebuilds the entire referenced graph with
`/p:ArtifactsPath` pointed at its own job directory, which relocates `obj/` for every project.
Nerdbank.GitVersioning writes its version file to a staging name and copies it into place, and
under the relocated path the staging file is never produced, so the copy fails.

The results were therefore reported as `NA` for every method, and the table in item 8 could not
have come from this source tree. This is the same failure mode as a published table for a
benchmark that was never wired up: the number is quoted, and the thing that was supposed to
produce it is broken. It is worth noting how it survived — `dotnet build` on the benchmark project
succeeds, because the failure happens only in the second, nested build that BenchmarkDotNet
performs at run time. A green build is not evidence that a benchmark runs.

Version stamping carries no information for a throwaway measurement host, so it is switched off
for that build alone, keyed on the job directory BenchmarkDotNet names, with CA1016 suppressed in
the same narrow scope because that rule exists to enforce the stamping just disabled. Ordinary
builds, packs and CI are untouched.

The fix has to live in `Directory.Build.props`. The generated project sets
`ImportDirectoryBuildTargets=false`, which suppresses the targets import, but its
`ImportDirectoryBuildProps=false` is declared in the project body, which MSBuild reads after it
has already imported the props file.

## 13. Benchmarks were pinned to a runtime the project does not ship

**Severity: moderate.**

Both benchmark classes carried `[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net90, ...)]` while
the project targeted `net10.0` and the published results were captioned .NET 10.0.12. A hardcoded
runtime drifts away from the framework the project actually builds, and the caption then names a
runtime that was never measured.

The moniker is gone; the job follows the project''s target framework, and the project derives its
frameworks from the shipped-library list, so there is no second place for the value to drift from.

## 14. Benchmarks never covered `net8.0`, which the package ships

**Severity: moderate.**

The benchmark project used the application framework list, which is the current release only,
while the library ships `net8.0` as well. Performance claims are made per runtime, so a figure
measured on one framework was being quoted for a package that shipped two.

This is the same defect as a test suite that skips a shipped framework, recorded against Sannr and
Rapp earlier in this programme, and it is worth stating that it was not recognised as the same
defect until the benchmarks were looked at directly. The project now derives its frameworks from
`ToolkitBenchmarkTargetFrameworks`, which is defined as the shipped-library list.

It immediately earned its keep. AutoMappic is marginally faster than hand-written mapping on
.NET 10 and roughly 12% slower on .NET 8; only one of those two results was previously visible.

## 15. `Regex.Matches(...).Count` allocated a match collection in the CLI

**Severity: low.**

`AutoMappic.Cli` counted matches with `regex.Matches(text).Count`, which materialises a
`MatchCollection` purely to read its length. Replaced with `regex.Count(text)` (CA1875).

## 16. The CI coverage step passed a switch the test platform rejects

**Severity: high.**

The workflow ran `dotnet test --collect:"XPlat Code Coverage"`. `--collect` is a VSTest data-collector
switch; every test project here runs on Microsoft.Testing.Platform, which rejects unknown options and
exits with code 5 after executing zero tests. The CI workflow could not have passed on its first
run, on any runner.

The obvious fix, `--coverage`, did not work either: Prova's generated entry point registered no
platform extensions besides the dump providers, so the coverage extension was never loaded. That
was a Prova defect (Prova `known-issues.md` #29) and was fixed there.

**Fix:** CI uses `--coverage --coverage-output-format cobertura`, and `AutoMappic.Cli.Tests`
references `Microsoft.Testing.Extensions.CodeCoverage`. The exact CI command was run locally
against the fixed Prova: 532 tests passed and three Cobertura reports were written.

## 17. Version 0.7.0 would be rebuilt with different contents

**Severity: high.**

`version.json` still said `0.7.0`, which is already published. Nerdbank.GitVersioning treats
`main` as a public-release branch, so the first build after merging would have produced a package
named `0.7.0` with different contents from the one consumers already have. NuGet rejects the push,
and any feed that accepted it would serve two different packages under one version.

**Fix:** bumped to `0.8.0`, the next minor version, because this release changes public behaviour.

---

## 18. `-p:PublishAot=true` on the command line broke the generator project (NETSDK1207)

**Severity: high. Affected CI only — the AOT gate had never run to completion.**

`aot-validation.yml` passed `-p:PublishAot=true` to `dotnet publish`. The flag was redundant, since
the target project already declares `PublishAot`, and it was harmful: a `-p:` switch on the command
line creates a **global property**, and MSBuild propagates global properties into every
`ProjectReference` it builds. `AutoMappic.Generator` targets `netstandard2.0`, which cannot be
AOT-compiled, so the run failed with `error NETSDK1207: Ahead-of-time compilation is not supported
for the target framework.`

The same property set inside a project file does not flow across a `ProjectReference`. That
asymmetry is why it never reproduced locally.

**Fix:** the flag is removed. AOT stays configured in the project file. `dotnet publish` on a
multi-targeted project also requires an explicit framework (NETSDK1129), so the publish step now
passes `--framework net10.0` — on the publish step only, since adding it to the solution-wide build
would break the `netstandard2.0` generator.

---

## 19. The IL-warning list was split on its commas (MSB1006)

**Severity: high. Affected CI only.**

The same step passed `-p:WarningsAsErrors=IL2026,IL2046,IL2062,...`. The dotnet CLI splits `-p:`
values on commas, so every code after the first was parsed as a separate switch and the run failed
with `MSBUILD : error MSB1006: Property is not valid. Switch: IL2046` before compiling anything.
The gate had therefore never enforced a single trim or AOT warning as an error. A bare `;` is no
better, because it is the property separator.

**Fix:** the codes are joined with `%3B`, the escaped semicolon, which reaches MSBuild as one
property value.

---

## 20. The test suite depends on a version of Prova that was never published

**Severity: high. Not fixable inside this repository.**

`Directory.Packages.props` pins `Prova` to `0.6.0`. That version exists only in the local NuGet
cache of the machine this review was carried out on. nuget.org's newest published Prova is `0.5.0`,
so restore on any other machine fails:

```
error NU1102: Unable to find package Prova with version (>= 0.6.0)
```

Every local build succeeded because the cache was warm. The first CI run on a hosted runner, with a
cold cache, failed at restore — before compiling a line.

The pin is not the mistake. Pinning back to `0.5.0` fails to compile with several hundred `CS0246`
errors, because the APIs the tests use were added in `0.6.0`. The dependency is correct and the
*release* is what is missing.

**Fix:** none available here. Prova `0.6.0` must be published to nuget.org — its
`Directory.Build.props` already declares `<Version>0.6.0</Version>` and its tag-triggered
`publish.yml` will produce it — after which this repository restores unchanged. Recorded rather
than worked around, because pinning to a version that cannot compile, or vendoring a copy, would
hide a real release gap.

---

# Open

Nothing is open in AutoMappic, except the external dependency recorded as issue 20: CI cannot go
green until Prova `0.6.0` is published to nuget.org. That is a release step in another repository,
not an unresolved defect in this one.

Two caveats belong here rather than in the table, because neither is a defect and both limit what
the entries above are worth:

* Every result recorded here was produced on a single Windows ARM64 machine. CI has now been run on
  GitHub-hosted x64 runners, which is how issues 18, 19 and 20 were found, but it cannot complete
  until issue 20 is resolved, so the x64 and Linux results are not yet confirmed.
* The `net11.0` leg is opt-in via `IncludePreviewTargetFramework`. It has been exercised on the
  same machine with SDK `11.0.100-rc.1.26425.128` (restore, build and every test, `net8.0`,
  `net10.0` and `net11.0`), with no failures. A release candidate is not a release; the leg
  should be re-run against the GA SDK.
## 21. Generated projections did not compile for a flattened non-nullable value type

Flattening resolves a nested member into a null-propagating path and appends a fallback, e.g.
`Metadata?.LastLogin ?? (default!)`. The projection emitter then rewrites `?.` to `.`, because
EF cannot translate null propagation. That rewrite invalidated the fallback it left behind: with
the chain no longer nullable, `??` cannot be applied when the member is a non-nullable value
type. The emitter also appended `!` via `TrimEnd('!') + "!"`, which cannot see a `!` inside a
parenthesised fallback, so `(default!)` became `(default!)!`.

For a `DateTime` member the generated line was:

```csharp
MetadataLastLogin = source.Metadata.LastLogin ?? (default!)!,
```

which fails to compile with CS0019 ("Operator '??' cannot be applied to operands of type
'DateTime' and 'default'") and CS8715 ("Duplicate null suppression operator"). The fallback is
also redundant in a projection: a null navigation projects to SQL NULL rather than throwing.

The whole generator test suite passed throughout, because **no test compiled the generated
output** - every one of them asserted on the emitted text. The defect only appeared in a
consumer's build, and was found when the AOT publish job failed on `samples/AotBenchmark`.

Fixed by routing both emit sites through a single `SourceEmitter.ToProjectionExpression`, which
drops the `?? (default!)` guard when it strips `?.` and appends `!` only when one is not already
present. `ProjectionCompilesTests` now compiles the generator's output and asserts on compiler
diagnostics; reverting the fix fails three of its four tests with exactly CS0019 and CS8715.

## 22. The CLI shipped a dependency with nine high-severity advisories

`AutoMappic.Cli` depends on the MSBuild packages, which drag in
`System.Security.Cryptography.Xml` 9.0.0 transitively. That version carries nine known
high-severity advisories (GHSA-23rf-6693-g89p, GHSA-37gx-xxp4-5rgx, GHSA-6588-8gv4-xfgh,
GHSA-8q5v-6pqq-x66h, GHSA-cvvh-rhrc-wg4q, GHSA-g8r8-53c2-pm3f, GHSA-mmjf-rqrv-855v,
GHSA-w3x6-4m5h-cxqf and one further advisory). Nothing in AutoMappic calls the package, but it
was still resolved into the shipped tool.

Fixed by pinning a patched version. Note that the first attempted pin, 10.0.0, is vulnerable to
all nine advisories as well - a local vulnerability audit reported it clean, because the package
mirror used for local builds does not carry nuget.org's advisory data. Only the CI audit caught
it. 10.0.10 is the first 10.x release patched for every one of the nine; 10.0.12 is pinned as
the first such release available, matching the 10.0.12 BCL packages already in use.

The lesson worth keeping: a local audit against a mirror is not evidence of a clean result.

## 23. The AOT and trim gate measured the tests instead of the library

`aot-validation.yml` ran the analyzers over `AutoMappic.sln` with the IL codes promoted to
errors. That gated the result on every project in the repository and reported 2672 errors, of
which 2562 came from `AutoMappic.Tests`: the tests exercise the reflective fallback deliberately,
so each call correctly reports the `RequiresUnreferencedCode` and `RequiresDynamicCode` that the
fallback declares. That is the annotations working, not a defect. The samples, benchmarks and CLI
contributed the same kind of noise, and the two `netstandard2.0` generator projects do not
support these analyzers at all - they only emit NETSDK1210.

The gate therefore could not fail for a real reason: the signal from the shipped library was
buried under 96% noise. Scoped to `src/AutoMappic.Core`, which builds clean under the same
switches.

## Supported frameworks

AutoMappic multi-targets `net8.0` (LTS) and `net10.0` (current); `net8.0` was previously skipped
entirely. `net11.0` is built and tested in CI behind an opt-in switch so preview regressions surface
during the preview window, but it is not shipped in the released package until it is a supported
release.

---

## Reporting

Security-relevant issues should follow [SECURITY.md](../SECURITY.md) rather than being filed as
public issues.
