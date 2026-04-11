trait Foo(T:! Sized) {}
trait Bar: Foo(i32) {}

impl Foo(bool) for i32 {}
impl Bar for i32 {}

fn main() -> i32 {
    5
}
