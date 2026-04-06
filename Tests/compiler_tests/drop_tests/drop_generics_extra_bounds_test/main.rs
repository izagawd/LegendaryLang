use Std.Core.Marker.Drop;
trait Foo {}

struct Wrapper(T:! type) {
    val: T
}

impl[T:! Foo] Drop for Wrapper(T) {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
