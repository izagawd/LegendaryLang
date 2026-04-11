use Std.Ops.Drop;
trait Foo {}

struct Wrapper(T:! type) {
    val: T
}

impl[T:! Foo] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
