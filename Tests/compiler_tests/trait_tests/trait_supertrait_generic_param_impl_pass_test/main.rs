trait Bar(T:! type) {}
trait Foo(T:! type) : Bar(T) {}

struct MyType {
    val: i32
}

impl Bar(i32) for MyType {}
impl Foo(i32) for MyType {}

fn main() -> i32 {
    42
}
