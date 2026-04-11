# LegendaryLang

A compiled programming language that targets native machine code via LLVM. It draws syntax inspiration from Carbon and Rust, featuring ownership semantics, trait-based polymorphism, compile-time generics, and a reference system with shared and mutable borrows.

## Table of Contents

- [Basics](#basics)
- [Primitive Types](#primitive-types)
- [Variables and Expressions](#variables-and-expressions)
- [Control Flow](#control-flow)
- [Functions](#functions)
- [Structs](#structs)
- [Enums](#enums)
- [Generics](#generics)
- [Traits](#traits)
- [Inherent Impls](#inherent-impls)
- [Associated Types](#associated-types)
- [Supertraits](#supertraits)
- [Operator Overloading](#operator-overloading)
- [Type Inference](#type-inference)
- [Move Semantics and Copy](#move-semantics-and-copy)
- [Drop and RAII](#drop-and-raii)
- [References and Borrowing](#references-and-borrowing)
- [Box and Heap Allocation](#box-and-heap-allocation)
- [Qualified Trait Calls](#qualified-trait-calls)
- [Imports](#imports)
- [Standard Library](#standard-library)

---

## Basics

Every program needs a `main` function that returns `i32`:

```
fn main() -> i32 {
    0
}
```

The last expression in a block is its return value — no semicolon needed. An explicit `return` is also supported:

```
fn main() -> i32 {
    return 42;
}
```

Functions that return nothing omit the return type:

```
fn do_nothing() {
}
```

## Primitive Types

| Type    | Description                  |
|---------|------------------------------|
| `i32`   | 32-bit signed integer        |
| `u8`    | 8-bit unsigned integer       |
| `usize` | Pointer-sized unsigned int   |
| `bool`  | Boolean (`true` / `false`)   |

All primitive types implement `Copy`.

## Variables and Expressions

Variables are declared with `let`. Type annotations are optional when the type can be inferred:

```
fn main() -> i32 {
    let x = 5;
    let y: i32 = 10;
    x + y
}
```

Variables can be shadowed:

```
fn main() -> i32 {
    let a = 5;
    let a = 10;
    a               // 10
}
```

Block expressions produce values:

```
fn main() -> i32 {
    let result = {
        let x = 3;
        let y = 4;
        x + y
    };
    result          // 7
}
```

Arithmetic operators: `+`, `-`, `*`, `/`. Comparison: `<`, `>`, `==`.

## Control Flow

### If / Else

`if` is an expression — both branches produce a value:

```
fn abs(n: i32) -> i32 {
    if n < 0 {
        0 - n
    } else {
        n
    }
}
```

### While Loops

```
fn sum_to(n: i32) -> i32 {
    let total = 0;
    let i = 0;
    while i < n {
        i = i + 1;
        total = total + i;
    }
    total
}
```

### Match Expressions

Pattern matching on enums (see [Enums](#enums)):

```
enum Direction {
    North,
    South,
    East,
    West
}

fn to_degrees(d: Direction) -> i32 {
    match d {
        Direction.North => 0,
        Direction.South => 180,
        Direction.East  => 90,
        Direction.West  => 270
    }
}
```

Wildcard `_` catches everything else:

```
fn is_north(d: Direction) -> i32 {
    match d {
        Direction.North => 1,
        _ => 0
    }
}
```

### Matching Through References

When the scrutinee is a reference to an enum, pattern bindings become references to the payload fields instead of copies:

```
enum Holder {
    Val(i32)
}

fn set_inner(h: &mut Holder, v: i32) {
    match h {
        Holder.Val(x) => *x = v,   // x is &mut i32, not i32
        _ => {}
    }
}

fn main() -> i32 {
    let h = Holder.Val(0);
    set_inner(&mut h, 77);
    match h {
        Holder.Val(v) => v,         // by value — v is i32
        _ => 0
    }
}
```

## Functions

### Basic Functions

```
fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

### Recursion

```
fn factorial(n: i32) -> i32 {
    if n < 2 { 1 } else { n * factorial(n - 1) }
}
```

### Compile-Time Parameters

Functions can accept types as compile-time parameters using `:!` in `()`:

```
fn identity(T:! type, x: T) -> T {
    x
}

fn main() -> i32 {
    identity(i32, 42)
}
```

Compile-time parameters can be constrained with trait bounds:

```
fn add_things(T:! Addable, a: T, b: T) -> T {
    Addable.Add(a, b)
}
```

## Structs

### Definition and Construction

Structs are constructed with the `make` keyword:

```
struct Point {
    x: i32,
    y: i32
}

fn main() -> i32 {
    let p = make Point { x: 3, y: 7 };
    p.x + p.y      // 10
}
```

### Nested Structs

```
struct Inner { val: i32 }
struct Outer { inner: Inner }

fn main() -> i32 {
    let o = make Outer { inner: make Inner { val: 42 } };
    o.inner.val
}
```

### Generic Structs

Generic parameters are declared with `()` using `:!` for compile-time types:

```
struct Wrapper(T:! type) {
    val: T
}

fn main() -> i32 {
    let w = make Wrapper(i32) { val: 42 };
    w.val
}
```

Multiple type parameters:

```
struct Pair(A:! type, B:! type) {
    first: A,
    second: B
}

fn main() -> i32 {
    let p = make Pair(i32, i32) { first: 10, second: 20 };
    p.first + p.second
}
```

### Structs with Lifetimes

Implicit lifetime parameters go in `[]`. Generic type parameters go in `()`. They can be combined:

```
struct Holder['a] {
    r: &'a mut i32
}

struct RefWrapper['a](T:! type) {
    field: &'a T
}
```

**Important:** `[]` in struct (and enum) definitions is reserved for lifetime parameters only. Generic type parameters must use `()`:

```
struct Foo(T:! type) { val: T }          // Correct
struct Foo[T:! type] { val: T }          // Error: [] is for lifetimes only
struct Foo['a](T:! type) { r: &'a T }   // Correct: lifetimes in [], generics in ()
```

## Enums

### Unit Variants

```
enum Color {
    Red,
    Green,
    Blue
}

fn main() -> i32 {
    let c = Color.Green;
    match c {
        Color.Red   => 1,
        Color.Green => 2,
        Color.Blue  => 3
    }
}
```

### Tuple Variants

Variants can carry data:

```
enum Shape {
    Circle(i32),
    Rectangle(i32, i32),
    None
}

fn area(s: Shape) -> i32 {
    match s {
        Shape.Circle(r) => r * r,
        Shape.Rectangle(w, h) => w * h,
        Shape.None => 0
    }
}
```

### Generic Enums

`Option(T)` is provided by the standard library and auto-imported:

```
fn main() -> i32 {
    let x = Option.Some(7);
    match x {
        Option.Some(val) => val,
        Option.None => 0
    }
}
```

Custom generic enums with multiple type parameters:

```
enum Either(A:! type, B:! type) {
    Left(A),
    Right(B)
}

fn main() -> i32 {
    let x = Either(i32, i32).Left(5);
    match x {
        Either.Left(a) => a,
        Either.Right(b) => b
    }
}
```

## Generics

LegendaryLang uses Carbon-style generics with two parameter categories:

- **`[]` — Implicit / Deduced**: Lifetimes and compile-time parameters that are **always inferred** from call arguments. The caller never provides these directly — the compiler deduces them. Lifetimes must come first.
- **`()` — Explicit**: Compile-time and runtime parameters that are passed at the call site.

### Deduced Parameters

Parameters in `[]` are invisible to the caller — they are inferred from the types of the runtime arguments:

```
fn identity[T:! type](x: T) -> T {
    x
}

fn main() -> i32 {
    identity(42)        // T inferred as i32 from the argument
}
```

The caller writes `identity(42)`, not `identity[i32](42)` — `[]` parameters can never be specified explicitly.

### Explicit Parameters

When placed in `()`, parameters must be provided at the call site:

```
fn get_val(T:! HasVal) -> i32 {
    T.Val()
}

fn main() -> i32 {
    get_val(i32)        // T explicitly passed
}
```

### Mixing Deduced and Explicit

Implicit `[]` comes first, then explicit `()`. Lifetimes are the first to be in `[]`.:

```
fn transform['a, T:! type](x: T) -> T {
    x
}
```

### Constrained Generics

Use `:!` with a trait bound:

```
fn double[T:! Copy](x: T) -> i32 {
    42
}
```

Multiple bounds with `+`:

```
use Std.Ops.Add;

fn add_twice[T:! Add(T) + Copy](a: T, b: T) -> (T as Add(T)).Output {
    a + b
}
```

### Sized and MetaSized

By default, `T:! type` implies `T: Sized` — only types with a known compile-time size are accepted. This means unsized types like `str` and slices (`[T]`) cannot be used where `T:! type` is expected.

To accept both sized and unsized types, use `T:! MetaSized`:

```
fn get_metadata[T:! MetaSized](ptr: *shared T) -> (T as MetaSized).Metadata {
    // T can be sized (metadata = ()) or unsized (metadata = usize)
}
```

If you need both `MetaSized` and `Sized`, the explicit `Sized` wins — `T` is always sized:

```
fn example[T:! MetaSized + Sized](x: T) -> T { x }  // OK: Sized is explicit
```

Every type implements `MetaSized`. The associated type `Metadata` is:
- `()` for sized types (structs, enums, primitives)
- `usize` for `str` and `[T]` (byte length / element count)

The `Sized` trait has `MetaSized` as a supertrait. Both are compiler-implemented — manual impls are rejected.

**Structs, enums, and tuples** are `Sized` if and only if all their fields / variant payloads / components are `Sized`. If any field is unsized, the entire type becomes unsized. This propagates recursively:

```
struct Inner[T:! MetaSized] { val: T }
struct Outer[T:! MetaSized] { inner: Inner(T) }

// Outer(i32) is Sized — Inner(i32) is Sized — i32 is Sized
// Outer(str) is unsized — Inner(str) is unsized — str is unsized

struct RefWrapper[T:! MetaSized] { ptr: &T }
// RefWrapper(str) IS Sized — &str is a fat pointer, always sized
```

In **trait definitions**, `Self` is assumed potentially unsized by default. To require `Self` to be sized (e.g., to take `self` by value), add `Sized` as a supertrait:

```
trait Foo {
    fn Bro(input: Self) {} // Error: Self might be unsized
}

trait Foo: Sized {
    fn Bro(input: Self) {} // OK: Self is guaranteed Sized
}
```

## Traits

### Defining Traits

```
trait Greet {
    fn hello() -> i32;
}
```

### Implementing Traits

```
impl Greet for i32 {
    fn hello() -> i32 { 42 }
}
```

### Method Calls

Methods that take `self: &Self` can be called with dot syntax:

```
trait HasValue {
    fn value(self: &Self) -> i32;
}

struct Foo { val: i32 }

impl HasValue for Foo {
    fn value(self: &Foo) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.value()
}
```

### Static Trait Method Calls

Methods without `self` are called on the type directly:

```
trait Default {
    fn default() -> Self;
}

impl Default for i32 {
    fn default() -> i32 { 0 }
}

fn main() -> i32 {
    i32.default()
}
```

### Generic Traits

Traits can have compile-time parameters:

```
trait Converter(Target:! type) {
    fn convert(input: Self) -> Target;
}

impl Converter(bool) for i32 {
    fn convert(input: i32) -> bool {
        true
    }
}
```

### Using Traits as Bounds

```
fn get_value(T:! HasValue, thing: T) -> i32 {
    HasValue.get_value(thing)
}
```

## Inherent Impls

Structs can have methods defined directly (not through a trait):

```
struct Counter { val: i32 }

impl Counter {
    fn get(self: &Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let c = make Counter { val: 42 };
    c.get()
}
```

### Static Methods and `Self`

```
struct Point { x: i32, y: i32 }

impl Copy for Point {}

impl Point {
    fn new(x: i32, y: i32) -> Self {
        make Point { x: x, y: y }
    }

    fn sum(self: &Self) -> i32 {
        self.x + self.y
    }
}

fn main() -> i32 {
    let p = Point.new(3, 7);
    p.sum()                     // 10
}
```

### Generic Inherent Impls

```
struct Wrapper(T:! type) { val: T }

impl[T:! Copy] Wrapper(T) {
    fn get_val(self: &Self) -> T {
        self.val
    }
}

fn main() -> i32 {
    let w : Wrapper(i32) = make Wrapper(i32) { val: 99 };
    w.get_val()
}
```

## Associated Types

Traits can declare associated types:

```
trait Producer {
    let Output :! type;
    fn produce() -> Self.Output;
}

impl Producer for i32 {
    let Output :! type = i32;
    fn produce() -> (Self as Producer).Output {
        42
    }
}
```

### With Operator Traits

```
use Std.Ops.Add;

fn add_em[T:! Add(T)](a: T, b: T) -> (T as Add(T)).Output {
    a + b
}

fn main() -> i32 {
    add_em(10, 20)
}
```

### Associated Type Constraints

```
use Std.Ops.Add;

fn add_same[T:! Add(T, Output = T) + Copy](a: T, b: T) -> T {
    a + b
}
```

## Supertraits

A trait can require another trait as a prerequisite:

```
trait Base {
    fn base_val() -> i32;
}

trait Sub: Base {
    fn sub_val() -> i32;
}

fn needs_sub(T:! Sub) -> i32 {
    T.base_val() + T.sub_val()
}

impl Base for i32 {
    fn base_val() -> i32 { 10 }
}

impl Sub for i32 {
    fn sub_val() -> i32 { 5 }
}

fn main() -> i32 {
    needs_sub(i32)              // 15
}
```

## Operator Overloading

Arithmetic operators dispatch through standard library traits in `Std.Ops`:

```
use Std.Ops.Add;

struct Vec2 { x: i32, y: i32 }

impl Copy for Vec2 {}

impl Add(Vec2) for Vec2 {
    let Output :! type = Vec2;
    fn Add(lhs: Vec2, rhs: Vec2) -> Vec2 {
        make Vec2 { x: lhs.x + rhs.x, y: lhs.y + rhs.y }
    }
}

fn main() -> i32 {
    let a = make Vec2 { x: 1, y: 2 };
    let b = make Vec2 { x: 3, y: 4 };
    let c = a + b;
    c.x + c.y                  // 10
}
```

Available operator traits: `Add`, `Sub`, `Mul`, `Div`.

## Type Inference

Generic type arguments in `[]` (deduced) are inferred from call arguments:

```
fn identity[T:! type](x: T) -> T { x }

fn main() -> i32 {
    identity(42)                // T = i32, inferred
}
```

Struct generic arguments can be inferred from field values:

```
struct Wrapper(T:! type) { val: T }

fn main() -> i32 {
    let w = make Wrapper { val: 42 };   // T = i32, inferred from val
    w.val
}
```

## Move Semantics and Copy

By default, values are **moved** on assignment. Using a moved value is a compile error:

```
struct Foo { val: i32 }

fn main() -> i32 {
    let a = make Foo { val: 5 };
    let b = a;                  // a is moved into b
    // a.val                    // ERROR: use after move
    b.val
}
```

Implement `Copy` to enable bitwise copies instead of moves:

```
struct Foo { val: i32 }

impl Copy for Foo {}

fn main() -> i32 {
    let a = make Foo { val: 5 };
    let b = a;                  // a is copied
    a.val + b.val               // both valid — 10
}
```

Primitive types (`i32`, `u8`, `usize`, `bool`) implement `Copy` automatically.

A type can only implement `Copy` if all its fields are also `Copy`. `Copy` and `Drop` are mutually exclusive.

## Drop and RAII

Implement `Drop` to run cleanup code when a value goes out of scope:

```
use Std.Ops.Drop;

struct Guard['a] {
    counter: &'a mut i32
}

impl['a] Drop for Guard['a] {
    fn Drop(self: &mut Self) {
        *self.counter = *self.counter + 1;
    }
}

fn main() -> i32 {
    let count = 0;
    {
        let g = make Guard { counter: &mut count };
        // g is dropped here — count becomes 1
    }
    count
}
```

Drop order is reverse declaration order. Moved values are not dropped. `Copy` and `Drop` cannot coexist on the same type.

### ManuallyDrop

Wrap a value in `ManuallyDrop` to suppress its destructor:

```
use Std.Mem.ManuallyDrop;
use Std.Ops.Drop;

// ManuallyDrop(T) prevents T's Drop from running
let _no_drop = ManuallyDrop.New(some_value);
```

## References and Borrowing

LegendaryLang has two reference kinds. The borrow checker ensures references don't outlive their data and prevents use-after-move.

| Kind | Syntax | Aliasing | Mutation |
|------|--------|----------|----------|
| Shared | `&T` | Multiple | Read-only (but can observe mutations from `&mut`) |
| Mutable | `&mut T` | Multiple | Read + Write |

```
fn increment(r: &mut i32) {
    *r = *r + 1;
}

fn main() -> i32 {
    let x = 0;
    increment(&mut x);
    increment(&mut x);
    x                       // 2
}
```

### Deref Assignment

Write through a reference using `*ref = value`:

```
fn main() -> i32 {
    let a = 0;
    let r = &mut a;
    *r = 42;
    a                       // 42
}
```

`&mut` can only reassign types that implement `MutReassign`:

```
use Std.Marker.MutReassign;

struct Point { x: i32, y: i32 }
impl Copy for Point {}
impl MutReassign for Point {}

fn main() -> i32 {
    let p = make Point { x: 0, y: 0 };
    let r = &mut p;
    *r = make Point { x: 10, y: 20 };  // OK — Point implements MutReassign
    p.x + p.y                          // 30
}
```

All primitives, references, and raw pointers implement `MutReassign`. Structs can implement it if all fields do. Enums can only implement it if they're flat (no payload variants).

### Auto-Deref

References are automatically dereferenced for field access and method calls:

```
struct Point { x: i32, y: i32 }
impl Copy for Point {}

fn main() -> i32 {
    let p = make Point { x: 3, y: 4 };
    let r = &p;
    r.x + r.y               // auto-deref — no need for (*r).x
}
```

### Lifetimes

Lifetime parameters on structs declare that the type borrows from external data:

```
struct Holder['a] {
    val: &'a mut i32
}
```

Functions can have explicit lifetime annotations to control borrow propagation:

```
fn first['a](a: &'a i32, b: &i32) -> &'a i32 {
    a
}

fn main() -> i32 {
    let x = 10;
    let y = 20;
    let r = first(&x, &y);   // r borrows from x, not y
    *r
}
```

Without explicit annotations, **lifetime elision** applies: if a function has exactly one reference parameter, the return value is assumed to borrow from it.

### Non-Lexical Lifetimes (NLL)

Borrows expire at the borrower's **last use**, not at scope exit:

```
fn main() -> i32 {
    let x = 10;
    let r = &mut x;
    let val = *r;             // last use of r
    x + val                   // OK — r's borrow has expired
}
```

### Raw Pointers

Raw pointers (`*shared`, `*mut`) are the unsafe counterparts of references. They are used internally by `Box` and the allocator but bypass borrow checking:

```
// Raw pointers are primarily used through Box internals
let b: Box(i32) = Box.New(42);
*b                            // auto-derefs through Box's Deref impl
```

## Box and Heap Allocation

`Box(T)` allocates a value on the heap. It automatically frees memory when dropped:

```
fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    *b                          // dereference — 42
}
```

`Box` auto-derefs through method calls:

```
struct Foo { x: i32, y: i32 }

impl Copy for Foo {}

impl Foo {
    fn sum(self: &Self) -> i32 { self.x + self.y }
}

fn main() -> i32 {
    let b: Box(Foo) = Box.New(make Foo { x: 10, y: 32 });
    b.sum()                     // auto-derefs Box(Foo) to call Foo.sum
}
```

## Qualified Trait Calls

When disambiguation is needed, use `(Type as Trait).method()` syntax:

```
trait Foo {
    fn value() -> i32;
}

impl Foo for i32 {
    fn value() -> i32 { 42 }
}

fn main() -> i32 {
    (i32 as Foo).value()
}
```

This works with generic traits too:

```
(Vec2 as Std.Ops.Add(Vec2)).Add(a, b)
```

## Imports

Use `use` to bring items into scope by their full module path:

```
use Std.Ops.Add;
use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;
```

After importing, use the short name:

```
use Std.Ops.Add;

impl Add(i32) for MyType {
    // ...
}
```

Without importing, use the full path:

```
impl Std.Ops.Add(i32) for MyType {
    // ...
}
```

`Copy`, `Box`, `MutReassign`, and `Option` are auto-imported and always available without a `use` statement.

### The `crate` Keyword

`crate` expands to the package root — the top-level directory passed to the compiler. For a package at `code/`, `crate` equals `code`. Use it to reference items by their full path within the package:

```
enum Dir { Left, Right }
use crate.Dir.Left;
use crate.Dir.Right;

fn main() -> i32 {
    match Dir.Left {
        Left => 1,
        Right => 2,
        _ => 0
    }
}
```

Note: `main.rs` files adopt their parent directory as the module name. A file at `code/main.rs` has module `code`, not `code.main`.

## Standard Library

### `Std.Core`

| Item        | Description                                                  |
|-------------|--------------------------------------------------------------|
| `Option(T)` | Generic enum with `Some(T)` and `None` variants. Auto-imported. |

### `Std.Marker`

| Item           | Description                                                  |
|----------------|--------------------------------------------------------------|
| `Copy`         | Marker trait — types are bitwise-copied instead of moved.    |
| `MutReassign`  | Marker trait — types can be reassigned through `&mut` references. |

### `Std.Ops`

| Item  | Description |
|-------|-------------|
| `Drop` | Destructor trait — `fn Drop(self: &mut Self)` runs on scope exit. |
| `Add` | `+` operator — `trait Add(Rhs:! type) { let Output :! type; fn Add(lhs: Self, rhs: Rhs) -> Self.Output; }` |
| `Sub` | `-` operator — same shape as Add. |
| `Mul` | `*` operator — same shape as Add. |
| `Div` | `/` operator — same shape as Add. |
| `PartialEq` | `==` and `!=` operators — `fn Eq(lhs: &Self, rhs: &Rhs) -> bool`. |
| `PartialOrd` | `<`, `>`, `<=`, `>=` operators — extends PartialEq. |
| `TryInto` | Fallible type conversion — `fn try_into(self: Self) -> Option(T)`. |

All four arithmetic traits are implemented for `i32` with `Output = i32`.

### `Std.Primitive`

| Item               | Description                                        |
|--------------------|-----------------------------------------------------|
| `TryCastPrimitive` | Intrinsic for safe numeric casts with overflow checking. Used internally by `TryInto` impls. |

`TryInto` conversions are provided between `i32`, `u8`, and `usize`:

### `Std.Alloc`

| Item               | Description                                        |
|--------------------|----------------------------------------------------|
| `Box(T)`           | Heap-allocated smart pointer. Freed on drop.       |

### `Std.Mem`

| Item               | Description                                        |
|--------------------|----------------------------------------------------|
| `SizeOf(T)`        | Returns the size of type `T` in bytes.             |
| `AlignOf(T)`       | Returns the alignment of type `T` in bytes.        |
| `ManuallyDrop(T)`  | Wrapper that suppresses the inner value's destructor. |

### `Std.Ptr`

| Item               | Description                                        |
|--------------------|----------------------------------------------------|
| `PtrWrite(dst, val)` | Writes a value to a raw pointer destination.     |
| `PtrAsU8(ptr)`     | Casts a typed raw pointer to `*mut u8`.           |
| `DestructPtr(ptr)` | Destructs the value at a pointer (Drop + field drops). |

### `Std.Deref`

| Item         | Description                                                  |
|--------------|--------------------------------------------------------------|
| `Receiver`   | Base trait for deref — declares `let Target :! type`.               |
| `Deref`      | `fn deref(self: &Self) -> &Self.Target`                      |
| `DerefMut`   | `fn deref_mut(self: &mut Self) -> &mut Self.Target`          |

## Syntax Quick Reference

```
// Variable declaration
let x = 42;
let y: i32 = 10;

// Function
fn add(a: i32, b: i32) -> i32 { a + b }

// Struct
struct Point { x: i32, y: i32 }
let p = make Point { x: 1, y: 2 };

// Generic struct
struct Wrapper(T:! type) { val: T }
let w = make Wrapper(i32) { val: 42 };

// Enum (Option is in std, auto-imported)
enum Either(A:! type, B:! type) { Left(A), Right(B) }
let x = Option.Some(5);

// Match
match x {
    Option.Some(v) => v,
    Option.None => 0
}

// Generics — deduced (implicit)
fn id[T:! type](x: T) -> T { x }

// Generics — explicit
fn convert(T:! type, U:! type, x: T) -> U { ... }

// Trait
trait Foo { fn bar() -> i32; }
impl Foo for i32 { fn bar() -> i32 { 0 } }

// Inherent impl
impl Point { fn origin() -> Self { make Point { x: 0, y: 0 } } }

// References
let r = &x;                   // shared
let r = &mut x;               // mutable
*r = 42;                      // deref assignment
r.field                       // auto-deref field access

// Lifetimes
struct Holder['a] { val: &'a mut i32 }
fn first['a](a: &'a i32, b: &i32) -> &'a i32 { a }

// Reference pattern matching
match &my_enum {
    Variant(x) => *x,         // x is &T, not T
    _ => 0
}

// Import
use Std.Ops.Add;

// Qualified call
(i32 as Foo).bar()
```
