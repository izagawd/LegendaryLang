trait Bar(T:! Sized) {}
trait Foo(T:! Sized) : Bar(T) {}

struct MyType {
    val: i32
}

impl Bar(i32) for MyType {}
impl Foo(i32) for MyType {}

fn main() -> i32 {
    42
}
