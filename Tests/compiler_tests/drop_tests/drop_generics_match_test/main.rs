use Std.Core.Marker.Drop;
struct Wrapper(T:! type) {
    val: T
}

impl[T:! type] Drop for Wrapper(T) {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    {
        let w = make Wrapper(i32) { val : 42 };
    }
    0
}
