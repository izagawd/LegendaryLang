use Std.Core.Marker.Drop;
struct Simple {
    x: i32
}

impl[T:! type] Drop for Simple {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
