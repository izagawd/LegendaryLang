trait Bar(T:! Sized) {}
trait Foo(T:! Sized) : Bar(T) {}

struct MyType {
    val: i32
}

impl Foo(i32) for MyType {}

fn main() -> i32 {
    5
}
