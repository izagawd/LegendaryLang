use Std.Ops.PartialOrd;
use Std.Ops.PartialEq;

struct Weight { g: i32 }
impl Copy for Weight {}

impl PartialEq(Weight) for Weight {
    fn Eq(lhs: Weight, rhs: Weight) -> bool { lhs.g == rhs.g }
}

impl PartialOrd(Weight) for Weight {
    fn Lt(lhs: Weight, rhs: Weight) -> bool { lhs.g < rhs.g }
    fn Gt(lhs: Weight, rhs: Weight) -> bool { lhs.g > rhs.g }
}

fn max_of[T:! Sized +PartialOrd(T) + Copy](a: T, b: T) -> T {
    if a > b { a } else { b }
}

fn main() -> i32 {
    let a = make Weight { g: 100 };
    let b = make Weight { g: 200 };
    let heavier = max_of(a, b);
    heavier.g
}
