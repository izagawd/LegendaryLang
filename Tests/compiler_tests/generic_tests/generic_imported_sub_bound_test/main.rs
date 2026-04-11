use Std.Ops.Sub;

fn sub_things[T:! Sized +Sub(T, Output = T) + Copy](a: T, b: T) -> T {
    a - b
}

fn main() -> i32 {
    sub_things(50, 8)
}
