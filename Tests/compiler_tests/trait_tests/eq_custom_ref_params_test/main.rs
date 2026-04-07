use Std.Ops.PartialEq;

struct Num { val: i32 }
impl Copy for Num {}

impl PartialEq(Num) for Num {
    fn Eq(lhs: &Num, rhs: &Num) -> bool {
        lhs.val == rhs.val
    }
}

fn main() -> i32 {
    let a = make Num { val: 42 };
    let b = make Num { val: 42 };
    let c = make Num { val: 99 };
    let r1 = if a == b { 1 } else { 0 };
    let r2 = if a != c { 10 } else { 0 };
    r1 + r2
}
