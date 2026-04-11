use Std.Ops.Drop;
struct Wrapper(T:! Sized) {
    val: T
}

impl[T:! Sized, U:! Sized] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
