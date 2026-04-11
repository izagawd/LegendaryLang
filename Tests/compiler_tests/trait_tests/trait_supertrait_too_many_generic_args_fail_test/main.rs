trait Foo(T:! Sized) {}
trait Bar: Foo(i32, bool) {}

fn main() -> i32 {
    5
}
