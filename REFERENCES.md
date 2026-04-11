# Reference Types in LegendaryLang

LegendaryLang has four reference kinds, each with distinct guarantees about aliasing and mutation.

## The Four Reference Kinds

### `&T` — Shared Immutable Reference
The default reference. Provides shared, read-only access to a value. Multiple `&T` references to the same value can exist simultaneously.

`&T` **can observe mutations** made through a `&mut T` to the same value. This is the key difference from `&const T`.

```
let x = 10;
let a = &x;
let b = &x;      // OK — multiple & allowed
let c = &mut x;   // OK — & and &mut can coexist
let val = *a;     // might see mutations made through c
```

Raw pointer equivalent: `*shared T`

### `&mut T` — Shared Mutable Reference
A shared reference that allows mutation. Multiple `&mut T` references can exist alongside `&T` references.

Cannot coexist with `&const T` — because `&const` guarantees no mutations will be observed, and `&mut` could mutate the value, violating that guarantee.

```
let x = 10;
let m = &mut x;
let r = &x;       // OK — & and &mut can coexist
let c = &const x;  // ERROR — &const and &mut are incompatible
```

Raw pointer equivalent: `*mut T`

### `&const T` — Shared Immutable Reference (No Observable Mutations)
A shared reference that guarantees the value **will not be mutated** for the lifetime of the reference. This is a stronger guarantee than `&T`.

Can coexist with `&T` but **not** with `&mut T` — because `&mut` could mutate the value, breaking the no-mutation guarantee.

```
let x = 10;
let c = &const x;
let r = &x;        // OK — & and &const can coexist
let m = &mut x;     // ERROR — &const and &mut are incompatible
```

Raw pointer equivalent: `*const T`

### `&mut T` — Unique Reference
Guarantees that **nothing else references the value**. Provides exclusive access — no other reference of any kind can coexist with `&mut T`.

```
let x = 10;
let u = &mut x;
let r = &x;        // ERROR — &mut is incompatible with everything
let m = &mut x;     // ERROR
let c = &const x;   // ERROR
```

Raw pointer equivalent: `*mut T`

## Compatibility Table

| Existing ↓ / New → | `&`  | `&mut` | `&const` | `&mut` |
|---------------------|------|--------|----------|---------|
| `&`                 | ✅   | ✅     | ✅       | ❌      |
| `&mut`              | ✅   | ✅     | ❌       | ❌      |
| `&const`            | ✅   | ❌     | ✅       | ❌      |
| `&mut`             | ❌   | ❌     | ❌       | ❌      |

## Capability Hierarchy (Deref Chain Narrowing)

When accessing fields through references, the outer reference narrows what the inner reference can produce:

```
&mut  >  &mut, &const  >  &
```

- Through `&Wrapper`, you can only produce `&` access to fields
- Through `&mut Wrapper`, you can produce up to `&mut` access
- Through `&const Wrapper`, you can produce up to `&const` access
- Through `&mut Wrapper`, you can produce any level of access

Example: if `Wrapper` has field `inner: &mut Holder`, accessing through `&Wrapper` narrows the effective capability to `&` — you cannot call a method requiring `&mut Self` on the inner field.

```
struct Wrapper['a] { inner: &'a mut Holder }

fn through_shared(w: &Wrapper) -> i32 {
    w.inner.get()       // OK — & method through & wrapper
    // w.inner.modify() // ERROR — &mut method through & wrapper
}

fn through_uniq(w: &mut Wrapper) -> i32 {
    w.inner.modify()    // OK — &mut method through &mut wrapper
}
```

## Borrowing Rules

When a variable is borrowed, the original variable is restricted based on the reference kind:

- **`&mut` borrow**: The source variable cannot be used in any way until the `&mut` reference is no longer alive (NLL — last use determines liveness).
- **`&mut` borrow**: Can coexist with `&` borrows. Incompatible with `&const` borrows.
- **`&const` borrow**: Can coexist with `&` borrows. Incompatible with `&mut` borrows.
- **`&` borrow**: Can coexist with `&`, `&mut`, and `&const` borrows.

### Borrows Through Structs and Enums

Types with lifetime parameters (e.g., `Holder['a]`) carry borrows from their reference fields. The borrow persists as long as the struct/enum value is alive:

```
struct Holder['a] { val: &'a mut i32 }

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &mut x };
    // x = 20;     // ERROR — x is uniquely borrowed by h
    DropNow(h);     // h is consumed, borrow released
    x               // OK — x is accessible again
}
```

### Borrows Through Function Returns

When a function takes a reference parameter and returns a value that could hold a borrow (reference or lifetime-bearing type), the return value inherits the borrow:

```
fn wrap(r: &mut i32) -> Holder {
    make Holder { val: r }
}

fn main() -> i32 {
    let x = 10;
    let h = wrap(&mut x);   // h borrows from x
    // let y = x;             // ERROR — x borrowed by h
    DropNow(h);               // borrow released
    x                         // OK
}
```

With explicit lifetime annotations, borrow tracking follows the annotated lifetimes. Without annotations, single-reference-input elision applies: if a function has exactly one reference parameter, the return value is assumed to borrow from it.

## Deref Traits

Each reference kind has a corresponding deref trait:

| Reference | Deref Trait  | Method               | Returns     |
|-----------|-------------|----------------------|-------------|
| `&T`      | `Deref`      | `deref(&Self)`       | `&Target`   |
| `&const T`| `DerefConst` | `deref_const(&const Self)` | `&const Target` |
| `&mut T`  | `DerefMut`   | `deref_mut(&mut Self)`   | `&mut Target`   |
| `&mut T` | `DerefUniq`  | `deref_uniq(&mut Self)`  | `&mut Target`  |

Smart pointers (like `Box(T)`) implement these traits to enable transparent field access and method calls through the pointer.

## MutReassign Trait

By default, reassigning a value through `&mut` is forbidden. Only `&mut` allows arbitrary reassignment. To allow reassignment through `&mut`, a type must implement the `MutReassign` marker trait:

```
use Std.Marker.MutReassign;

struct Point { x: i32, y: i32 }
impl Copy for Point {}
impl MutReassign for Point {}

fn main() -> i32 {
    let p = make Point { x: 0, y: 0 };
    let r = &mut p;
    *r = make Point { x: 10, y: 20 };  // OK
    p.x + p.y
}
```

### Rules

- All primitive types (`i32`, `u8`, `usize`, `bool`), references, and raw pointers implement `MutReassign` automatically.
- Structs can implement `MutReassign` only if **all fields** implement `MutReassign`.
- Enums can implement `MutReassign` only if they are **flat** (all unit variants, no payload).
- `&mut` always allows reassignment regardless of `MutReassign`.
- `MutReassign` is auto-imported (like `Copy` and `Box`).

### Why?

`&mut` is shared — multiple `&mut` references can exist to the same value. Allowing arbitrary reassignment through a shared mutable reference could cause issues with types that have complex internal structure. `MutReassign` is an opt-in guarantee that the type is safe to replace wholesale through shared mutation.

## Reference Enum Pattern Matching

When matching on a reference to an enum, pattern bindings become references to the payload fields rather than copies:

```
enum Holder {
    Val(i32)
}

fn read_inner(h: &Holder) -> i32 {
    match h {
        Holder.Val(x) => *x,    // x is &i32, deref to get i32
        _ => 0
    }
}

fn set_inner(h: &mut Holder, v: i32) {
    match h {
        Holder.Val(x) => *x = v, // x is &mut i32, can write through it
        _ => {}
    }
}
```

The binding's reference kind matches the scrutinee's reference kind:

| Scrutinee | Binding type for field `T` |
|-----------|---------------------------|
| `Enum` (by value) | `T` (copy) |
| `&Enum` | `&T` |
| `&mut Enum` | `&mut T` |
| `&const Enum` | `&const T` |
| `&mut Enum` | `&mut T` |

## Auto-Reborrow

When a `&mut` reference is passed to a function, it is automatically reborrowed rather than moved. This allows reusing the reference after the call:

```
fn set(r: &mut i32, val: i32) {
    *r = val;
}

fn main() -> i32 {
    let x = 0;
    let r = &mut x;
    set(r, 10);     // auto-reborrow — r is NOT consumed
    set(r, 20);     // can still use r
    *r              // 20
}
```

This matches Rust's `&mut` auto-reborrow behavior. Without auto-reborrow, `&mut` would be moved on first use (since it's non-Copy), making it impractical for exclusive references.

Note: `let p = r` still moves `&mut` — auto-reborrow only applies to function/method arguments.

## Non-Lexical Lifetimes (NLL)

Borrows expire at the borrower's **last use**, not at the end of the enclosing scope. This allows the source variable to be used again after the borrower is no longer needed:

```
fn main() -> i32 {
    let x = 10;
    let r = &mut x;
    let val = *r;        // last use of r — borrow expires here
    x + val              // OK — x is accessible again
}
```

Without NLL, `x + val` would fail because `r` is still in scope.

### NLL Eligibility

| Borrower type | Borrow expiration |
|---------------|-------------------|
| Reference (`&T`, `&mut T`, etc.) | Last use (NLL) |
| Copy type | Last use (NLL) |
| Non-Copy, non-reference, **without Drop** | Scope exit (moved = released early) |
| Non-Copy, non-reference, **with Drop** | Scope exit (Drop accesses borrowed data) |

For non-Copy non-reference borrowers, the borrow persists until the borrower is moved or goes out of scope. However, if the borrowed source is never used again, no conflict arises — the borrow is harmless.

## Borrow Propagation Through Generics

When a type with lifetime parameters (e.g., `Holder['a]`) is passed through a generic function, the borrow relationships are preserved based on the **type definition**:

```
struct Holder['a] {
    r: &'a mut i32
}

fn PassAround[T:! type](input: T) -> T {
    input
}

fn main() -> i32 {
    let x = 5;
    let h = make Holder { r: &mut x };
    let h2 = PassAround(h);    // h2 still borrows x
    // x = 10;                  // ERROR — x borrowed by h2
    0
}
```

The compiler detects this by checking the concrete return type after generic substitution. When `T` resolves to `Holder['a]` — a type whose definition declares lifetime parameters — the compiler knows the return value carries borrows and propagates them from the input arguments.

This works through chained calls (`Pass2(Pass1(h))`) and releases correctly when the borrower is consumed (`DropNow(h2)`).
