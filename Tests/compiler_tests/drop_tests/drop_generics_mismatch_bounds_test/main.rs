use Std.Ops.Drop;
trait Foo {}

struct Wrapper(T:! Sized +Foo) {
    val: T
}

impl[T:! Sized] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
