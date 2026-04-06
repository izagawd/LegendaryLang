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

### `&uniq T` — Unique Reference
Guarantees that **nothing else references the value**. Provides exclusive access — no other reference of any kind can coexist with `&uniq T`.

```
let x = 10;
let u = &uniq x;
let r = &x;        // ERROR — &uniq is incompatible with everything
let m = &mut x;     // ERROR
let c = &const x;   // ERROR
```

Raw pointer equivalent: `*uniq T`

## Compatibility Table

| Existing ↓ / New → | `&`  | `&mut` | `&const` | `&uniq` |
|---------------------|------|--------|----------|---------|
| `&`                 | ✅   | ✅     | ✅       | ❌      |
| `&mut`              | ✅   | ✅     | ❌       | ❌      |
| `&const`            | ✅   | ❌     | ✅       | ❌      |
| `&uniq`             | ❌   | ❌     | ❌       | ❌      |

## Capability Hierarchy (Deref Chain Narrowing)

When accessing fields through references, the outer reference narrows what the inner reference can produce:

```
&uniq  >  &mut, &const  >  &
```

- Through `&Wrapper`, you can only produce `&` access to fields
- Through `&mut Wrapper`, you can produce up to `&mut` access
- Through `&const Wrapper`, you can produce up to `&const` access
- Through `&uniq Wrapper`, you can produce any level of access

Example: if `Wrapper` has field `inner: &uniq Holder`, accessing through `&Wrapper` narrows the effective capability to `&` — you cannot call a method requiring `&uniq Self` on the inner field.

```
struct Wrapper['a] { inner: &'a uniq Holder }

fn through_shared(w: &Wrapper) -> i32 {
    w.inner.get()       // OK — & method through & wrapper
    // w.inner.modify() // ERROR — &uniq method through & wrapper
}

fn through_uniq(w: &uniq Wrapper) -> i32 {
    w.inner.modify()    // OK — &uniq method through &uniq wrapper
}
```

## Borrowing Rules

When a variable is borrowed, the original variable is restricted based on the reference kind:

- **`&uniq` borrow**: The source variable cannot be used in any way until the `&uniq` reference is no longer alive (NLL — last use determines liveness).
- **`&mut` borrow**: Can coexist with `&` borrows. Incompatible with `&const` borrows.
- **`&const` borrow**: Can coexist with `&` borrows. Incompatible with `&mut` borrows.
- **`&` borrow**: Can coexist with `&`, `&mut`, and `&const` borrows.

### Borrows Through Structs and Enums

Types with lifetime parameters (e.g., `Holder['a]`) carry borrows from their reference fields. The borrow persists as long as the struct/enum value is alive:

```
struct Holder['a] { val: &'a uniq i32 }

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &uniq x };
    // x = 20;     // ERROR — x is uniquely borrowed by h
    DropNow(h);     // h is consumed, borrow released
    x               // OK — x is accessible again
}
```

### Borrows Through Function Returns

When a function takes a reference parameter and returns a value that could hold a borrow (reference or lifetime-bearing type), the return value inherits the borrow:

```
fn wrap(r: &uniq i32) -> Holder {
    make Holder { val: r }
}

fn main() -> i32 {
    let x = 10;
    let h = wrap(&uniq x);   // h borrows from x
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
| `&uniq T` | `DerefUniq`  | `deref_uniq(&uniq Self)`  | `&uniq Target`  |

Smart pointers (like `Box(T)`) implement these traits to enable transparent field access and method calls through the pointer.
