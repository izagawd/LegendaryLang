use Std.Core.Marker.Drop;
trait Foo {}

struct Wrapper(T:! Foo) {
    val: T
}

impl[T:! type] Drop for Wrapper(T) {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
