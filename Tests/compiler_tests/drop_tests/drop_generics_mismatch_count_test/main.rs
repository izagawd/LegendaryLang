use Std.Core.Marker.Drop;
struct Wrapper(T:! type) {
    val: T
}

impl[T:! type, U:! type] Drop for Wrapper(T) {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
