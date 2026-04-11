use Std.Ops.Drop;
struct Simple {
    x: i32
}

impl[T:! Sized] Drop for Simple {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
