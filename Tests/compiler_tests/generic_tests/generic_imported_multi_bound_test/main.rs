use Std.Ops.Add;
use Std.Ops.Sub;

fn add_then_sub[T:! Sized +Add(T, Output = T) + Sub(T, Output = T) + Copy](a: T, b: T, c: T) -> T {
    a + b - c
}

fn main() -> i32 {
    add_then_sub(30, 20, 8)
}
