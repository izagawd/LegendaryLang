use Std.Ops.PartialEq;

fn are_equal[T:! Sized +PartialEq(T)](a: &T, b: &T) -> bool {
    a == b
}

fn main() -> i32 {
    let x = 42;
    let y = 42;
    let z = 99;
    let r1 = if are_equal(&x, &y) { 1 } else { 0 };
    let r2 = if are_equal(&x, &z) { 10 } else { 0 };
    r1 + r2
}
