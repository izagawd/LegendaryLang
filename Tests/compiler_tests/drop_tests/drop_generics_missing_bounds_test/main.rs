use Std.Ops.Drop;
trait Bar {}
trait Baz {}

struct Wrapper(T:! Sized +Bar + Baz) {
    val: T
}

impl[T:! Sized +Bar] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
