# LegendaryLang

A compiled programming language that targets native machine code via LLVM. It draws syntax inspiration from Carbon and Rust, featuring ownership semantics, trait-based polymorphism, compile-time generics, and a four-tier reference system.

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

Implicit lifetime parameters go in `[]`:

```
struct Holder['a] {
    r: &'a uniq i32
}
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

```
enum Option(T:! type) {
    Some(T),
    None
}

fn main() -> i32 {
    let x = Option.Some(7);
    match x {
        Option.Some(val) => val,
        Option.None => 0
    }
}
```

Multiple type parameters:

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

- **`[]` — Implicit / Deduced**: Lifetimes and compile-time parameters that are inferred. Lifetimes must come first.
- **`()` — Explicit**: Compile-time and runtime parameters that are passed at the call site.

### Deduced Parameters

When placed in `[]`, compile-time parameters are inferred from usage:

```
fn identity[T:! type](x: T) -> T {
    x
}

fn main() -> i32 {
    identity(42)        // T inferred as i32
}
```

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

Implicit `[]` comes first, then explicit `()`:

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
use Std.Core.Ops.Add;

fn add_twice[T:! Add(T) + Copy](a: T, b: T) -> (T as Add(T)).Output {
    a + b
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
    let w = make Wrapper(i32) { val: 99 };
    w.get_val()
}
```

## Associated Types

Traits can declare associated types:

```
trait Producer {
    type Output;
    fn produce() -> Self.Output;
}

impl Producer for i32 {
    type Output = i32;
    fn produce() -> (Self as Producer).Output {
        42
    }
}
```

### With Operator Traits

```
use Std.Core.Ops.Add;

fn add_em[T:! Add(T)](a: T, b: T) -> (T as Add(T)).Output {
    a + b
}

fn main() -> i32 {
    add_em(10, 20)
}
```

### Associated Type Constraints

```
use Std.Core.Ops.Add;

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

Arithmetic operators dispatch through standard library traits in `Std.Core.Ops`:

```
use Std.Core.Ops.Add;

struct Vec2 { x: i32, y: i32 }

impl Copy for Vec2 {}

impl Add(Vec2) for Vec2 {
    type Output = Vec2;
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
use Std.Core.Marker.Drop;

struct Guard['a] {
    counter: &'a uniq i32
}

impl['a] Drop for Guard['a] {
    fn Drop(self: &uniq Self) {
        *self.counter = *self.counter + 1;
    }
}

fn main() -> i32 {
    let count = 0;
    {
        let g = make Guard { counter: &uniq count };
        // g is dropped here — count becomes 1
    }
    count
}
```

Drop order is reverse declaration order. Moved values are not dropped. `Copy` and `Drop` cannot coexist on the same type.

### ManuallyDrop

Wrap a value in `ManuallyDrop` to suppress its destructor:

```
use Std.Core.Alloc.ManuallyDrop;
use Std.Core.Marker.Drop;

// ManuallyDrop(T) prevents T's Drop from running
let _no_drop = make ManuallyDrop(MyType) { val: some_value };
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
(Vec2 as Std.Core.Ops.Add(Vec2)).Add(a, b)
```

## Imports

Use `use` to bring items into scope by their full module path:

```
use Std.Core.Ops.Add;
use Std.Core.Marker.Drop;
use Std.Core.Alloc.ManuallyDrop;
```

After importing, use the short name:

```
use Std.Core.Ops.Add;

impl Add(i32) for MyType {
    // ...
}
```

Without importing, use the full path:

```
impl Std.Core.Ops.Add(i32) for MyType {
    // ...
}
```

`Copy` and `Box` are auto-imported and always available without a `use` statement.

## Standard Library

### `Std.Core.Marker`

| Item   | Description                                                  |
|--------|--------------------------------------------------------------|
| `Copy` | Marker trait — types are bitwise-copied instead of moved.    |
| `Drop` | Destructor trait — `fn Drop(self: &uniq Self)` runs on scope exit. |

### `Std.Core.Ops`

| Item  | Operator | Signature |
|-------|----------|-----------|
| `Add` | `+`      | `trait Add(Rhs:! type) { type Output; fn Add(lhs: Self, rhs: Rhs) -> Self.Output; }` |
| `Sub` | `-`      | `trait Sub(Rhs:! type) { type Output; fn Sub(lhs: Self, rhs: Rhs) -> Self.Output; }` |
| `Mul` | `*`      | `trait Mul(Rhs:! type) { type Output; fn Mul(lhs: Self, rhs: Rhs) -> Self.Output; }` |
| `Div` | `/`      | `trait Div(Rhs:! type) { type Output; fn Div(lhs: Self, rhs: Rhs) -> Self.Output; }` |

All four are implemented for `i32` with `Output = i32`.

### `Std.Core.Alloc`

| Item               | Description                                        |
|--------------------|----------------------------------------------------|
| `Box(T)`           | Heap-allocated smart pointer. Freed on drop.       |
| `ManuallyDrop(T)`  | Wrapper that suppresses the inner value's destructor. |
| `SizeOf(T)`        | Returns the size of type `T` in bytes.             |
| `AlignOf(T)`       | Returns the alignment of type `T` in bytes.        |

### `Std.Core.Deref`

| Item         | Description                                                  |
|--------------|--------------------------------------------------------------|
| `Receiver`   | Base trait for deref — declares `type Target`.               |
| `Deref`      | `fn deref(self: &Self) -> &Self.Target`                      |
| `DerefConst` | `fn deref_const(self: &const Self) -> &const Self.Target`    |
| `DerefMut`   | `fn deref_mut(self: &mut Self) -> &mut Self.Target`          |
| `DerefUniq`  | `fn deref_uniq(self: &uniq Self) -> &uniq Self.Target`       |

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

// Enum
enum Option(T:! type) { Some(T), None }
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

// Import
use Std.Core.Ops.Add;

// Qualified call
(i32 as Foo).bar()
```
