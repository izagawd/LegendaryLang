trait Bar(T:! type) {}
trait Foo(T:! type) : Bar(T) {}

struct MyType {
    val: i32
}

impl Foo(i32) for MyType {}

fn main() -> i32 {
    5
}
