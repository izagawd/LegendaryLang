trait Foo(T:! type) {}
trait Bar(T:! type): Foo(T) {}

impl Foo(i32) for i32 {}
impl Bar(i32) for i32 {}

fn main() -> i32 {
    42
}
