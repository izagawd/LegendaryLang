use Std.Ops.Drop;
struct Wrapper(T:! type) {
    val: T
}

impl[T:! type] Drop for Wrapper(T) {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    {
        let w = make Wrapper(i32) { val : 42 };
    }
    0
}
