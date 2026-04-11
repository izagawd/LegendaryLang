use Std.Ops.Drop;
trait Foo {}

struct Wrapper(T:! Foo) {
    val: T
}

impl[T:! type] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
