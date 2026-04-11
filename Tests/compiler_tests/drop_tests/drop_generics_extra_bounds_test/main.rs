use Std.Ops.Drop;
trait Foo {}

struct Wrapper(T:! Sized) {
    val: T
}

impl[T:! Sized +Foo] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
