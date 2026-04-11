use Std.Ops.Drop;
struct Wrapper(T:! Sized) {
    val: T
}

impl[T:! Sized] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    {
        let w = make Wrapper(i32) { val : 42 };
    }
    0
}
