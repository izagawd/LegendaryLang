trait Foo(T:! type) {}
trait Bar: Foo(i32) {}

impl Foo(i32) for i32 {}
impl Bar for i32 {}

fn main() -> i32 {
    42
}
