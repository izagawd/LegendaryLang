use Std.Ops.PartialEq;
use Std.Ops.PartialOrd;

struct Weight { g: i32 }

impl PartialEq(Weight) for Weight {
    fn Eq(lhs: Weight, rhs: Weight) -> bool { lhs.g == rhs.g }
}

impl PartialOrd(Weight) for Weight {
    fn Lt(lhs: Weight, rhs: Weight) -> bool { lhs.g < rhs.g }
    fn Gt(lhs: Weight, rhs: Weight) -> bool { lhs.g > rhs.g }
}

fn main() -> i32 {
    let a = make Weight { g: 5 };
    let b = make Weight { g: 10 };
    let r1 = a < b;
    let r2 = a > b;
    let r3 = a <= b;
    let r4 = a >= b;
    let r5 = a == b;
    let r6 = a != b;
    let result = 0;
    if r1 { result = result + 1; };
    if r6 { result = result + 10; };
    result
}
