# Reference Types in LegendaryLang

LegendaryLang has two reference kinds.

## The Two Reference Kinds

### `&T` — Shared Immutable Reference
The default reference. Provides shared, read-only access to a value. Multiple `&T` references to the same value can exist simultaneously.

`&T` **can observe mutations** made through a `&mut T` to the same value.

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

```
let x = 10;
let m = &mut x;
let r = &x;       // OK — & and &mut can coexist
let m2 = &mut x;  // OK — multiple &mut allowed
```

Raw pointer equivalent: `*mut T`

## Compatibility Table

| Existing ↓ / New → | `&`  | `&mut` |
|---------------------|------|--------|
| `&`                 | ✅   | ✅     |
| `&mut`              | ✅   | ✅     |

All reference kinds are compatible with each other. The borrow checker ensures references don't outlive their data and prevents use-after-move.

## Capability Hierarchy (Deref Chain Narrowing)

When accessing fields through references, the outer reference narrows what the inner reference can produce:

```
&mut  >  &
```

- Through `&Wrapper`, you can only produce `&` access to fields
- Through `&mut Wrapper`, you can produce up to `&mut` access

Example: if `Wrapper` has field `inner: &mut Holder`, accessing through `&Wrapper` narrows the effective capability to `&` — you cannot call a method requiring `&mut Self` on the inner field.

```
struct Wrapper['a] { inner: &'a mut Holder }

fn through_shared(w: &Wrapper) -> i32 {
    w.inner.get()       // OK — & method through & wrapper
    // w.inner.modify() // ERROR — &mut method through & wrapper
}

fn through_mut(w: &mut Wrapper) -> i32 {
    w.inner.modify()    // OK — &mut method through &mut wrapper
}
```

## Borrowing Rules

The borrow checker ensures that:

- References cannot outlive the data they point to (no dangling references).
- Values that have been moved cannot be accessed.

All borrow kinds are compatible — `&` and `&mut` can coexist freely.

### Borrows Through Structs and Enums

Types with lifetime parameters (e.g., `Holder['a]`) carry borrows from their reference fields. The borrow persists as long as the struct/enum value is alive:

```
struct Holder['a] { val: &'a mut i32 }

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &mut x };
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
    let h = wrap(&mut x);    // h borrows from x
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
| `&mut T`  | `DerefMut`   | `deref_mut(&mut Self)`   | `&mut Target`   |

Smart pointers (like `Gc(T)`) implement these traits to enable transparent field access and method calls through the pointer.

## MutReassign Trait

By default, reassigning a value through `&mut` is restricted to types that implement the `MutReassign` marker trait:

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
- `MutReassign` is auto-imported (like `Copy` and `Gc`).

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
| Reference (`&T`, `&mut T`) | Last use (NLL) |
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

fn PassAround[T:! Sized](input: T) -> T {
    input
}

fn main() -> i32 {
    let x = 5;
    let h = make Holder { r: &mut x };
    let h2 = PassAround(h);    // h2 still borrows x
    0
}
```

The compiler detects this by checking the concrete return type after generic substitution. When `T` resolves to `Holder['a]` — a type whose definition declares lifetime parameters — the compiler knows the return value carries borrows and propagates them from the input arguments.

This works through chained calls (`Pass2(Pass1(h))`) and releases correctly when the borrower is consumed (`DropNow(h2)`).
