use Std.Ops.Drop;
struct Wrapper(T:! type) {
    val: T
}

impl[T:! type, U:! type] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
