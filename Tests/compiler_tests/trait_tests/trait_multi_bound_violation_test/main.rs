use Std.Core.Ops.Add;
use Std.Core.Ops.Mul;
trait Adder {
    fn Add(a: Self, b: Self) -> Self;
}

trait Multiplier {
    fn Mul(a: Self, b: Self) -> Self;
}

impl Adder for i32 {
    fn Add(a: i32, b: i32) -> i32 {
        a + b
    }
}

impl Multiplier for i32 {
    fn Mul(a: i32, b: i32) -> i32 {
        a * b
    }
}

fn compute(T:! Adder + Multiplier + Copy, a: T, b: T) -> T {
    (T as Adder).Add((T as Multiplier).Mul(a, b), b)
}

fn main() -> i32 {
    compute(bool, true, false)
}
