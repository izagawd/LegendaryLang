# LegendaryLang Compiler — Session Handoff Document

## Repository
`/home/claude/LegendaryLang/` — C# compiler targeting LLVM, for a Rust/Carbon-inspired language.

## Previous Transcripts
All in `/mnt/transcripts/`:
- `2026-04-06-*` — Sessions 1-2: initial refactoring, traits, operators, borrow checking
- `2026-04-07-*` — Session 3: TryInto, inference, enums, fat pointer research
- `2026-04-08-04-47-32-*` — Session 4: tuples, fat pointers, str, MetaSized/Sized, trait scope
- **Current session (session 5)**: MetaSized/Sized completion, trait scope enforcement, method resolution restructure, Sized validation, lifetime field validation

## User Preferences (CRITICAL — enforce always)
- ALL scoped state uses `Stack<T>`, pushed/popped with `AddScope`/`PopScope`. No exceptions.
- NEVER duplicate logic. If X and Y are similar, they MUST share code.
- Before writing ANY new code path, check if an existing path does something similar. If so, generalize it.
- No redundant fields — derive from existing data.
- Data-driven design (e.g., all pointers have metadata — sized = empty tuple metadata).
- Minimum 5 tests per feature.
- Do NOT write quick hack fixes. Ask questions if unsure.
- Do NOT change tests to make them pass — fix the program. Need user approval to change tests.
- Struct/enum generics use `()` not `[]`. `[]` is ONLY for lifetimes. Functions use `[]` for deduced params and `()` for explicit params.
- `using NUnit.Framework;` is ALWAYS needed in test files.

## Architecture Overview

### Type System
- **Primitives**: i32, u8, usize, bool — defined in `Std/Primitive.rs`, registered via `PrimitiveTypeGenerator`
- **str**: unsized primitive (`IsUnsized = true`), `StrTypeDefinition`
- **References**: 4 kinds — `&T` (shared), `&const T`, `&mut T`, `&uniq T`. Defined in `RefTypeDefinition`
- **Raw pointers**: 4 kinds — `*shared T`, `*const T`, `*mut T`, `*uniq T`. Defined in `RawPtrTypeDefinition`
- **Fat pointers**: `&const str` is `{ptr, usize}` struct. `PointerLikeType` handles thin vs fat.
- **Tuples**: `()` (void), `(T,)` (1-tuple), `(T, U)` (2-tuple). `TupleLangPath` + `TupleTypeDefinition`
- **Box**: `Std/Alloc.rs` — heap allocation, implements Deref traits

### MetaSized / Sized System (COMPLETED THIS SESSION)
- `trait MetaSized { let Metadata :! Copy; }` — universal, every type implements it
- `trait Sized: MetaSized {}` — only sized types
- Both are compiler-implemented — manual impls rejected in `ImplDefinition.Analyze`
- Synthetic impls generated in `GenerateMetaSizedAndSizedImpls()`:
  - Primitives, str: generated from TypeDefinitions (str gets Metadata=usize, others get Metadata=())
  - Pointer types: generated with UNBOUNDED T (pointers are always Sized)
  - Void tuple: explicit MetaSized+Sized impls
  - Structs/enums: MetaSized impls generated, but NO synthetic Sized impl — Sized is determined by field inspection in `TypeImplementsTrait`
  - Block-level types: found via `CollectTypeDefinitions` (recursive AST walk)
- `TypeImplementsTrait` handles Sized for structs/enums by checking all fields (with generic substitution via `SubstituteType` + `BuildGenericSubstitutions`)
- Recursion guard: `_sizedCheckVisited` HashSet

### Implicit Sized Bounds
- `T:! type` → implicit `T: Sized` (added by `BuildGenericBoundsWithImplicitSized`)
- `T:! MetaSized` → opt out, T can be unsized
- `T:! MetaSized + Sized` → Sized wins (explicit)
- `BuildGenericBoundsWithImplicitSized` is used by FunctionDefinition, ImplDefinition (×2), TraitDefinition — ONE shared method
- In traits, Self gets implicit MetaSized (universal) but NOT implicit Sized (Self might be unsized)
- Associated types get implicit Sized too: `let Output :! type` → Output: Sized

### Sized Validation
- `ValidateParamsSized(params, location, returnType?)` on SemanticAnalyzer — shared by FunctionDefinition and TraitDefinition
- Checks all params AND return type are Sized
- Struct fields: Sized determined at TypeImplementsTrait check time (not at definition)
- If a struct field `val: T` where T:! MetaSized → struct is unsized when T is unsized
- If a struct field `ptr: &T` where T:! MetaSized → struct is still Sized (reference is always Sized)
- Propagates recursively through nested structs/enums

### Trait Scope Enforcement (COMPLETED THIS SESSION)
- `_traitsInScope` — `Stack<List<LangPath>>` (List because NormalLangPath lacks GetHashCode)
- `ImportTrait(path)` / `IsTraitInScope(path)` — register/check
- Same-file traits auto-registered during Analyze loop
- `UseDefinition.Analyze` imports traits via `use`
- Method resolution priority (in `MethodCallKind`):
  1. `CollectImplCandidates(filter: inherent)` — inherent methods first
  2. `CollectImplCandidates(filter: in-scope traits)` — trait methods (error if ambiguous)
  3. Trait bounds for generic types
  4. Auto-deref chain, repeat from 1
- `Disambiguate` — returns null if multiple trait candidates match (error)
- `BuildImplResult` — shared MethodCallKind construction
- Operator syntactic sugars (`+`, `-`, `==`, etc.) do NOT need trait in scope

### Lifetime Field Validation (STARTED THIS SESSION — may have bugs)
- `NormalLangPath.LifetimeArgs` — stores lifetime args from `Bar['a]` (no longer erased)
- `VariableDefinition.Lifetime` — captures `LastParsedLifetime` from `&'a T`
- `EnumVariant.FieldLifetimes` — parallel to FieldTypes
- `ValidateFieldLifetimes` (shared by struct and enum):
  - Ref fields require lifetime params on the type
  - All declared lifetimes must be used
  - Nested type lifetime args must reference declared lifetimes
  - Ref field lifetimes must reference declared lifetimes

### usize
- `UsizeTypeDefinition.TargetPointerBits` — static settable property (default 64)
- Replaces `Environment.Is64BitProcess` hack

### Key Paths
- `SemanticAnalyzer.MetaSizedTraitPath` = `Std.Marker.MetaSized`
- `SemanticAnalyzer.SizedTraitPath` = `Std.Marker.Sized`
- `SemanticAnalyzer.ReceiverTraitPath` = `Std.Deref.Receiver`
- `LangPath.VoidBaseLangPath` = `()` (TupleLangPath with empty types)
- `LangPath.PrimitivePath` = `Std.Primitive`

### NormalLangPath WARNING
**NormalLangPath overrides Equals but NOT GetHashCode.** Never use HashSet<LangPath> or Dictionary<LangPath, ...>. Use List + `.Any(p => p.Equals(...))` instead.

## Std Library State

### Marker.rs
- `MetaSized { let Metadata :! Copy; }` — universal
- `Sized: MetaSized` — sized types only
- `Copy: Sized` — requires Sized
- `MutReassign` — reassignment through mut refs
- `Primitive` — marker for primitives
- Copy/MutReassign impls for all primitives, `()`, all ref/ptr types with `T:! MetaSized`

### Ops.rs
- `Drop { fn Drop(self: &uniq Self); }`
- `Add/Sub/Mul/Div(Rhs): Sized` — binary operators, take Self by value
- Impls for i32, u8, usize (all arithmetic ops)
- `PartialEq/Eq` — comparison by reference (`&Self`)
- `PartialOrd/Ord` — ordering by reference, default methods
- `TryInto(T): Sized` — fallible conversion

### Deref.rs
- `Receiver { type Target; }` — base deref trait
- `Deref/DerefConst/DerefMut/DerefUniq: Receiver` — 4 deref levels

### Other
- `Alloc.rs` — Box(T) with New, heap allocation, Deref impls
- `Mem.rs` — `SizeOf(T:! type) -> usize`, `AlignOf(T:! type) -> usize`, ManuallyDrop(T)
- `Primitive.rs` — TryCastPrimitive intrinsic, TryInto impls for i32↔u8↔usize
- `Core.rs` — `enum Option(T:! type) { Some(T), None }`
- `Ptr.rs` — currently empty, planned for PtrMetadata/FromRawParts

## Features Planned Before Bootstrap

1. **Slices** — `[T]` unsized type, `&[T]` fat pointer (data ptr + length)
   - `get_const`/`get_mut`/`len` methods
   - MetaSized with Metadata = usize

2. **`as` casting** — three rules, one change at a time:
   - `*A T as *B T` — change restriction only
   - `*A T as *A U` — change pointee only
   - `&A T as *A T` — ref to raw ptr (same restriction)
   - NO jumps: `*uniq i32 as *const u8` rejected, must do two casts

3. **`FromRawParts` family** (in Std/Ptr.rs):
   - `fn FromRawParts(T:! MetaSized, ptr: *shared (), metadata: (T as MetaSized).Metadata) -> *shared T`
   - Mut, Const, Uniq variants
   - T is explicit (in `()`) because can't infer from `*kind ()`

4. **`PtrMetadata`** (in Std/Ptr.rs):
   - `fn PtrMetadata[T:! MetaSized](ptr: *shared T) -> (T as MetaSized).Metadata`
   - T is deduced from the pointer

5. **`len()` method** on `str` and `[T]`

6. **Ptr arithmetic** — pointer offset/add operations

7. **Syscalls / console I/O** — write to stdout, read stdin

8. **Panic** — runtime abort with message

9. **Trait objects** — `&dyn Trait` with vtable dispatch

10. **AddAssign/SubAssign/etc operators** — compound assignment `+=`, `-=`

11. **Const self fields RFC** — immutable fields on self

12. **Embedded structs (Embeds trait)** — `embed Foo` in struct

13. **Leading comptime param inference** — user said "remind me later"

## Known Issues / Incomplete

1. **Lifetime field validation** — basic checks added but may have bugs. Full borrow checker integration for lifetime-bounded struct fields needs testing. The `nested_struct_missing_lifetime_fail_test` and `nested_struct_undeclared_lifetime_fail_test` may not work yet if the parser doesn't track lifetime arity on type definitions (i.e., knowing that `Bar` expects 1 lifetime param).

2. **Parser ambiguity** — `&const ()` in expression position is parsed as "take reference to empty tuple" not "reference type to void". Affects `SizeOf(&const ())`. Workaround: use `SizeOf(usize)` since usize is pointer-sized.

3. **Number literal inference** — `thin * 2` fails because `2` infers to i32, not usize. No cross-type arithmetic yet.

4. **Orphaned test files** — some `.rs` test files exist but aren't referenced by any C# test class:
   - `assoc_type_tests/assoc_self_qualified_test`
   - `assoc_type_tests/assoc_bare_in_impl_fail_test`
   - `trait_tests/assoc_type_qualified_return_test`
   - `trait_tests/assoc_type_generic_return_test`
   These have traits taking Self by value without Sized supertrait — they'd fail with the new Sized checks. User was told about these and said NOT to fix them without asking.

5. **`try_into` method renamed to `TryInto`** — done in this session

6. **Shorthand associated type paths** — `T.Output` in struct field types doesn't fully work for Sized checks (stays as 2-segment NormalLangPath, needs QualifiedAssocTypePath resolution)

## Test Infrastructure
- All tests: `Tests/compiler_tests/<category>/<test_name>/main.rs` (+ optional other .rs files)
- C# test classes in `Tests/*.cs`
- `AssertSuccess(path, expectedReturnValue)` — compile + run + check exit code
- `AssertFail<TError>(path)` — compile + expect specific error type
- Error types: `GenericSemanticError`, `ParseError`, `TraitBoundViolationError`, `TraitNotFoundError`, etc.
- Each test folder is a complete LegendaryLang program (can have multiple .rs files)
