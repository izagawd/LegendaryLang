use Std.Ops.Drop;
trait Bar {}
trait Baz {}

struct Wrapper(T:! Bar + Baz) {
    val: T
}

impl[T:! Bar] Drop for Wrapper(T) {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
