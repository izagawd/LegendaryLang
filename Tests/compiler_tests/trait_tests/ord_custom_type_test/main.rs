use Std.Ops.PartialEq;
use Std.Ops.PartialOrd;

struct Num { val: i32 }
impl Copy for Num {}

impl PartialEq(Num) for Num {
    fn Eq(lhs: Num, rhs: Num) -> bool {
        lhs.val == rhs.val
    }
}

impl PartialOrd(Num) for Num {
    fn Lt(lhs: Num, rhs: Num) -> bool {
        lhs.val < rhs.val
    }
    fn Gt(lhs: Num, rhs: Num) -> bool {
        lhs.val > rhs.val
    }
}

fn main() -> i32 {
    let a = make Num { val: 5 };
    let b = make Num { val: 10 };
    let r1 = if a < b { 1 } else { 0 };
    let r2 = if b > a { 10 } else { 0 };
    let r3 = if a == a { 100 } else { 0 };
    r1 + r2 + r3
}
