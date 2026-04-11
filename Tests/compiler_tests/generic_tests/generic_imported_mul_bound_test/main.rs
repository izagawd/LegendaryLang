use Std.Ops.Mul;

fn mul_things[T:! Sized +Mul(T, Output = T) + Copy](a: T, b: T) -> T {
    a * b
}

fn main() -> i32 {
    mul_things(6, 7)
}
